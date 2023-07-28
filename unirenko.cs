#region Usando declaraciones
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;

#endregion

namespace NinjaTrader.NinjaScript.BarsTypes
{
    public class UniRenkoBarsType : BarsType
    {
        // Variables de instancia
        private int barDirection;
        private double barHigh, barLow, barMax, barMin, fakeOpen, openOffset, reversalOffset, thisClose, tickSize, trendOffset;
        private long barVolume;
        private bool isNewSession, maxExceeded, minExceeded, calculatedOnce;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"UniRenko";
                Name = "UniRenko";
                BarsPeriod = new BarsPeriod { BarsPeriodType = (BarsPeriodType)2018, BarsPeriodTypeName = "UniRenko(2018)", Value = 1 };
                BuiltFrom = BarsPeriodType.Tick;
                DefaultChartStyle = Gui.Chart.ChartStyleType.CandleStick;
                DaysToLoad = 3;
                IsIntraday = true;
                IsTimeBased = false;
            }
            else if (State == State.Configure)
            {
                barDirection = 0;
                openOffset = 0;
                Properties.Remove(Properties.Find("BaseBarsPeriodType", true));
                Properties.Remove(Properties.Find("PointAndFigurePriceType", true));
                Properties.Remove(Properties.Find("ReversalType", true));

                SetPropertyName("BaseBarsPeriodValue", "Open Offset");
                SetPropertyName("Value", "Tick Trend");
                SetPropertyName("Value2", "Tick Reversal");

                Name = string.Format("{0} UniRenko T{0}R{1}O{2}", BarsPeriod.Value, BarsPeriod.Value2, BarsPeriod.BaseBarsPeriodValue);
            }
        }

        public override void ApplyDefaultBasePeriodValue(BarsPeriod period) { }

        /// <summary>
        /// > Los parámetros por defecto para unirenko
        /// dialog
        /// </summary>
        /// <param name="BarsPeriod">Pasamos la serie de velas...</param>
        public override void ApplyDefaultValue(BarsPeriod period)
        {
            period.BaseBarsPeriodValue = 1;
            period.Value = 1;
            period.Value2 = 10;
        }

        public override string ChartLabel(DateTime dateTime)
        {
            return dateTime.ToString("T", Core.Globals.GeneralOptions.CurrentCulture);
        }

        /// <summary>
        /// > Cuantos días cargamos por defecto al abrir un gráfico con unirenko
        /// </summary>
        /// <param name="BarsPeriod">La serie con la que queremos trabajar</param>
        /// <param name="TradingHours">Horario a obtener histórico, por defecto viene el defecto de cada instrumento</param>
        /// <param name="barsBack">Numero de velas para atrás, por defecto se ignora todo y devuelve dos días.</param>
        /// <returns>
        /// The number of days to look back.
        /// </returns>
        public override int GetInitialLookBackDays(BarsPeriod barsPeriod, TradingHours tradingHours, int barsBack)
        {
            return 2;
        }

        public override double GetPercentComplete(Bars bars, DateTime now)
        {
            return 1.0d;
        }

        // Método principal para agregar y actualizar las barras UniRenko
        /// <summary>
        /// > Si el cierre de la barra actual es mayor que el máximo de la barra anterior, actualice el
        /// el máximo de la barra actual al cierre de la barra actual
        /// </summary>
        /// <param name="Bars">El objeto de barras que se está actualizando.</param>
        /// <param name="open">El precio de apertura de la barra.</param>
        /// <param name="high">El precio más alto de la barra.</param>
        /// <param name="low">El mínimo de la barra.</param>
        /// <param name="close">El cierre de la barra actual.</param>
        /// <param name="DateTime">La fecha y hora de la barra.</param>
        /// <param name="volume">El volumen de la barra.</param>
        /// <param name="isBar">Este es un valor booleano que es verdadero si el punto de datos es una barra y
        /// falso si es un tick.</param>
        /// <param name="bid">El precio de oferta.</param>
        /// <param name="ask">El precio de venta de la barra.</param>
        protected override void OnDataPoint(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isBar, double bid, double ask)
        {
            // Inicializa el iterador de sesión si no está definido
            if (SessionIterator == null)
                SessionIterator = new SessionIterator(bars);

            // Verifica si es una nueva sesión
            isNewSession = SessionIterator.IsNewSession(time, isBar);

            if (isNewSession)                 SessionIterator.GetNextSession(time, isBar);

            // Calcular valores si el objeto Bars está en medio de una sesión
            if (!calculatedOnce)
            {
                tickSize = bars.Instrument.MasterInstrument.TickSize;
                trendOffset = bars.BarsPeriod.Value * tickSize;
                reversalOffset = bars.BarsPeriod.Value2 * tickSize;

                openOffset = Math.Ceiling((double)bars.BarsPeriod.BaseBarsPeriodValue * 1) * tickSize;

                barMax = close + (trendOffset * barDirection);
                barMin = close - (trendOffset * barDirection);
                calculatedOnce = true;
            }

            // Primera barra
            if ((bars.Count == 0) || bars.IsResetOnNewTradingDay && isNewSession)
            {
                tickSize = bars.Instrument.MasterInstrument.TickSize;
                trendOffset = bars.BarsPeriod.Value * tickSize;
                reversalOffset = bars.BarsPeriod.Value2 * tickSize;

                openOffset = Math.Ceiling((double)bars.BarsPeriod.BaseBarsPeriodValue * 1) * tickSize;

                barMax = close + (trendOffset * barDirection);
                barMin = close - (trendOffset * barDirection);

                AddBar(bars, close, close, close, close, time, volume);
            }

            // Barras posteriores
            else
            {
                maxExceeded = bars.Instrument.MasterInstrument.Compare(close, barMax) > 0 ? true : false;
                minExceeded = bars.Instrument.MasterInstrument.Compare(close, barMin) < 0 ? true : false;

                barHigh = bars.GetHigh(bars.Count - 1);
                barLow = bars.GetLow(bars.Count - 1);
                barVolume = bars.GetVolume(bars.Count - 1);

                // ¿Se excedió el rango definido?
                if (maxExceeded || minExceeded)
                {
                    thisClose = maxExceeded ? Math.Min(close, barMax) : minExceeded ? Math.Max(close, barMin) : close;
                    barDirection = maxExceeded ? 1 : minExceeded ? -1 : 0;
                    fakeOpen = thisClose - (openOffset * barDirection); // El Fake Open está en la mitad de la barra

                    // Cerrar barra actual
                    UpdateBar(bars, (maxExceeded ? thisClose : barHigh), (minExceeded ? thisClose : barLow), thisClose, time, volume);

                    // Agregar nueva barra
                    barMax = thisClose + ((barDirection > 0 ? trendOffset : reversalOffset));
                    barMin = thisClose - ((barDirection > 0 ? reversalOffset : trendOffset));

                    AddBar(bars, fakeOpen, (maxExceeded ? thisClose : fakeOpen), (minExceeded ? thisClose : fakeOpen), thisClose, time, volume);
                }

                // Barra actual en desarrollo
                else
                    UpdateBar(bars, (close > barHigh ? close : barHigh), (close < barLow ? close : barLow), close, time, volume);
            }

            bars.LastPrice = close;
        }
    }
}

