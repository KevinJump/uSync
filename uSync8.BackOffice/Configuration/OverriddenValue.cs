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

        public void Override(TObject value)
        {
            Value = value;
            IsOverridden = true;
        }

        public void SetDefaultValue(TObject defaultValue)
        {
            Value = defaultValue;
            IsOverridden = false;
        }

        public TObject Value { get; set; }
        public bool IsOverridden { get; internal set; }

        public static implicit operator TObject(OverriddenValue<TObject> value)
        {
            return value.Value;
        }
    }

}
