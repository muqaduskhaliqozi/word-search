using System;
using System.Linq;
using System.Reflection;

namespace RuStore.PayClient {

    public abstract class BaseFields {

        private FieldInfo[] GetFields(object obj) =>
            obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private FieldInfo[] GetFields() => GetFields(this);

        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) return false;

            var fields = GetFields();
            var otherFields = GetFields(obj);

            for (int i = 0; i < fields.Length; i++) {
                var thisValue = fields[i].GetValue(this);
                var otherValue = otherFields[i].GetValue(obj);
                if (!Equals(thisValue, otherValue)) return false;
            }

            return true;
        }

        public override int GetHashCode() {
            var fields = GetFields();
            var values = fields.Select(field => field.GetValue(this)).ToArray();

            return HashCode.Combine(values);
        }

        public override string ToString() {
            var fields = GetFields();
            var values = fields.Select(field => $"{field.Name}={field.GetValue(this)}").ToArray();

            return $"{GetType().Name}('{string.Join(", ", values)}')";
        }
    }
}
