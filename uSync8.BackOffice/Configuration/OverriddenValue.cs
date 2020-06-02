namespace uSync8.BackOffice.Configuration
{
    /// <summary>
    ///  Overridden value - will let us use the value - but when 
    ///  we load/save it we can work out if it's actually overriding the global
    ///  setting.
    /// </summary>
    public class OverriddenValue<TObject>
    {
        public OverriddenValue()
            : this(default(TObject), false)
        { }

        public OverriddenValue(TObject value, bool overridden)
        {
            Value = value;
            IsOverridden = overridden;
        }

        /// <summary>
        ///  Override the default value
        /// </summary>
        public void Override(TObject value)
        {
            Value = value;
            IsOverridden = true;
        }

        /// <summary>
        ///  Set value to the default value
        /// </summary>
        /// <param name="defaultValue"></param>
        public void SetDefaultValue(TObject defaultValue)
        {
            Value = defaultValue;
            IsOverridden = false;
        }

        /// <summary>
        ///  Value of setting
        /// </summary>
        public TObject Value { get; set; }

        /// <summary>
        ///  Is this value overriding (diffrent) from the global default for this value?
        /// </summary>
        public bool IsOverridden { get; internal set; }

        public static implicit operator TObject(OverriddenValue<TObject> value)
        {
            return value.Value;
        }
    }

}
