using System;

namespace VroomJs
{
    public class ClrObjectTemplate : ClrMemberTemplate
    {
        private bool _useNetToString;

        public ClrObjectTemplate(bool useNetToString = false,
            MissingPropertyHandling missingPropertyHandling = MissingPropertyHandling.Ignore)
            :base(missingPropertyHandling)
        {
            _useNetToString = useNetToString;
        }

        protected override (Type, object) GetTargetTypeAndObject(object obj) => (obj.GetType(), obj);

        internal override string ToString(IHostObjectCallbackContext context, object obj)
        {
            if (_useNetToString)
                return obj.ToString();

            return base.ToString(context, obj);
        }
    }
}
