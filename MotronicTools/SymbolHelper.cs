using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;


namespace MotronicTools
{
    public class SymbolHelper
    {

        private string _units = string.Empty;

        public string Units
        {
            get { return _units; }
            set { _units = value; }
        }


        private bool _MapAllowsNegatives = true;

        public bool MapAllowsNegatives
        {
            get { return _MapAllowsNegatives; }
            set { _MapAllowsNegatives = value; }
        }

        private float _correctionFactor = 1;

        public float CorrectionFactor
        {
            get { return _correctionFactor; }
            set { _correctionFactor = value; }
        }

        private float _correctionOffset = 0;

        public float CorrectionOffset
        {
            get { return _correctionOffset; }
            set { _correctionOffset = value; }
        }

        private float _minValue = 0;

        public float MinValue
        {
            get { return _minValue; }
            set { _minValue = value; }
        }
        private float _maxValue = 0;

        public float MaxValue
        {
            get { return _maxValue; }
            set { _maxValue = value; }
        }
        private float _peakValue = 0;

        public float PeakValue
        {
            get { return _peakValue; }
            set { _peakValue = value; }
        }
        private float _currentValue = 0;

        public float CurrentValue
        {
            get { return _currentValue; }
            set { _currentValue = value; }
        }

        private string m_description = string.Empty;

        public string Description
        {
            get { return m_description; }
            set { m_description = value; }
        }

        private int _M44DamosXAxisID = 0;

        public int M44DamosXAxisID
        {
            get { return _M44DamosXAxisID; }
            set { _M44DamosXAxisID = value; }
        }
        private int _M44DamosYAxisID = 0;

        public int M44DamosYAxisID
        {
            get { return _M44DamosYAxisID; }
            set { _M44DamosYAxisID = value; }
        }
        

        private string m_userDescription = string.Empty;

        public string UserDescription
        {
            get { return m_userDescription; }
            set { m_userDescription = value; }
        }

        private bool m_isAxisSymbol = false;

        public bool IsAxisSymbol
        {
            get { return m_isAxisSymbol; }
            set { m_isAxisSymbol = value; }
        }

        private int m_symbol_number = 0;

        public int Symbol_number
        {
            get { return m_symbol_number; }
            set { m_symbol_number = value; }
        }

        private float m_average_value = 0;

        public float Average_value
        {
            get { return m_average_value; }
            set { m_average_value = value; }
        }
        string m_category = "Undocumented";

        public string Category
        {
            get { return m_category; }
            set { m_category = value; }
        }

        bool m_isSixteenbits = false;

        private string m_zDescr = string.Empty;

        public string ZDescr
        {
            get { return m_zDescr; }
            set { m_zDescr = value; }
        }

        private string m_xDescr = string.Empty;

        public string XDescr
        {
            get { return m_xDescr; }
            set { m_xDescr = value; }
        }
        private string m_yDescr = string.Empty;

        public string YDescr
        {
            get { return m_yDescr; }
            set { m_yDescr = value; }
        }

        private float[] x_axisvalues;

        public float[] X_axisvalues
        {
            get { return x_axisvalues; }
            set { x_axisvalues = value; }
        }
        private float[] y_axisvalues;

        public float[] Y_axisvalues
        {
            get { return y_axisvalues; }
            set { y_axisvalues = value; }
        }


        private int m_x_axis_length = 0;

        public int X_axis_length
        {
            get { return m_x_axis_length; }
            set { m_x_axis_length = value; }
        }

        private int m_x_axis_address = 0;

        public int X_axis_address
        {
            get { return m_x_axis_address; }
            set { m_x_axis_address = value; }
        }

        private int m_y_axis_length = 0;

        public int Y_axis_length
        {
            get { return m_y_axis_length; }
            set { m_y_axis_length = value; }
        }

        private int m_y_axis_address = 0;

        public int Y_axis_address
        {
            get { return m_y_axis_address; }
            set { m_y_axis_address = value; }
        }

        public bool IsSixteenbits
        {
            get { return m_isSixteenbits; }
            set { m_isSixteenbits = value; }
        }

        private int m_cols = 1;

        public int Cols
        {
            get { return m_cols; }
            set { m_cols = value; }
        }
        private int m_rows = 1;

        public int Rows
        {
            get { return m_rows; }
            set { m_rows = value; }
        }

        int start_address = 0x00000;

        int flash_start_address = 0x00000;

        public int Flash_start_address
        {
            get { return flash_start_address; }
            set { flash_start_address = value; }
        }

        public Int32 Start_address
        {
            get { return start_address; }
            set { start_address = value; }
        }
        int length = 0x00;

        public int Length
        {
            get { return length; }
            set { length = value; }
        }
        string varname = string.Empty;

        public string Varname
        {
            get { return varname; }
            set { varname = value; }
        }

        private Color _color = Color.Black;

        public Color Color
        {
            get { return _color; }
            set { _color = value; }
        }
    }
}
