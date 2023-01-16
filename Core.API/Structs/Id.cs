namespace System
{
    /// <summary>
    /// Represents a globally unique identifier (GUID) with a 
    /// shorter string value. Sguid
    /// </summary>
    public struct Id
    {
        #region Static

        /// <summary>
        /// A read-only instance of the ShortGuid class whose value 
        /// is guaranteed to be all zeroes. 
        /// </summary>
        public static readonly Id Empty = new Id(Guid.Empty);

        #endregion

        #region Fields

        readonly Guid? _guidVal;
        readonly string _strVal;
        readonly int? _intVal;
        readonly long? _longVal;

        #endregion

        #region Contructors

        /// <summary>
        /// Creates a ShortGuid from a base64 encoded string
        /// </summary>
        /// <param name="value">The encoded guid as a 
        /// base64 string</param>
        public Id(string value)
        {
            _strVal = value;
            _guidVal = Decode(value);
            _intVal = 0;
            _longVal = 0;
        }

        public Id(int value)
        {
            _strVal = null;
            _guidVal = null;
            _intVal = value;
            _longVal = null;
        }

        public Id(long value)
        {
            _strVal = null;
            _guidVal = null;
            _intVal = null;
            _longVal = value;
        }

        /// <summary>
        /// Creates a ShortGuid from a Guid
        /// </summary>
        /// <param name="guid">The Guid to encode</param>
        public Id(Guid guid)
        {
            _strVal = Encode(guid);
            _guidVal = guid;
            _intVal = null;
            _longVal = null;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the underlying Guid
        /// </summary>
        public Guid GuidVal
        {
            get { return _guidVal ?? Guid.Empty; }
        }

        /// <summary>
        /// Gets the underlying base64 encoded string
        /// </summary>
        public string StrVal
        {
            get { return _strVal; }
        }

        public int IntVal
        {
            get { return _intVal ?? 0; }
        }

        public long LongVal
        {
            get { return _longVal ?? 0; }
        }

        #endregion

        #region ToString

        /// <summary>
        /// Returns the base64 encoded guid as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _strVal ?? _intVal?.ToString() ?? _longVal?.ToString();
        }

        #endregion

        #region Equals

        /// <summary>
        /// Returns a value indicating whether this instance and a 
        /// specified Object represent the same type and value.
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }
            else if (obj is Id id)
            {
                return this == id;
            }

            else if (obj is Guid guid)
            {
                return _guidVal?.Equals(guid) ?? false;
            }

            else if (obj is string str)
            {
                return _strVal?.Equals(str) ?? false;
            }

            else if (obj is int i32)
            {
                return _intVal?.Equals(i32) ?? false;
            }

            else if (obj is long i64)
            {
                return _longVal?.Equals(i64) ?? false;
            }
            return false;
        }


        #endregion

        #region GetHashCode

        /// <summary>
        /// Returns the HashCode for underlying Guid.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _guidVal.GetHashCode();
        }

        #endregion

        #region NewGuid

        /// <summary>
        /// Initialises a new instance of the ShortGuid class
        /// </summary>
        /// <returns></returns>
        public static Id NewGuid()
        {
            return new Id(Guid.NewGuid());
        }

        public static bool Gt(object obj1, object obj2)
        {
            return From(obj1) > From(obj2);
        }

        public static bool Ge(object obj1, object obj2)
        {
            return From(obj1) >= From(obj2);
        }

        public static bool Lt(object obj1, object obj2)
        {
            return From(obj1) < From(obj2);
        }

        public static bool Le(object obj1, object obj2)
        {
            return From(obj1) <= From(obj2);
        }

        public static Id From(object obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            var id = obj;
            if (id is int i32)
            {
                return new Id(i32);
            }
            else if (id is int i64)
            {
                return new Id(i64);
            }
            else if (id is string str)
            {
                return new Id(str);
            }
            else if (id is Guid guid)
            {
                return new Id(guid);
            }
            else
            {
                id = obj.GetType().GetProperty("Id")?.GetValue(obj);
                if (id is null)
                {
                    throw new InvalidCastException("Object does not contain Id field or Id field is null");
                }
                return From(id);
            }
        }

        #endregion

        #region Encode

        /// <summary>
        /// Creates a new instance of a Guid using the string value, 
        /// then returns the base64 encoded version of the Guid.
        /// </summary>
        /// <param name="value">An actual Guid string (i.e. not a ShortGuid)</param>
        /// <returns></returns>
        public static string Encode(string value)
        {
            Guid guid = new Guid(value);
            return Encode(guid);
        }

        /// <summary>
        /// Encodes the given Guid as a base64 string that is 22 
        /// characters long.
        /// </summary>
        /// <param name="guid">The Guid to encode</param>
        /// <returns></returns>
        public static string Encode(Guid guid)
        {
            string encoded = Convert.ToBase64String(guid.ToByteArray());
            encoded = encoded
                .Replace("/", "_")
                .Replace("+", "-");
            return encoded.Substring(0, 22);
        }

        #endregion

        #region Decode

        /// <summary>
        /// Decodes the given base64 string
        /// </summary>
        /// <param name="value">The base64 encoded string of a Guid</param>
        /// <returns>A new Guid</returns>
        public static Guid Decode(string value)
        {
            value = value
                .Replace("_", "/")
                .Replace("-", "+");
            byte[] buffer = Convert.FromBase64String(value + "==");
            return new Guid(buffer);
        }

        #endregion

        #region Operators

        /// <summary>
        /// Determines if both ShortGuids have the same underlying 
        /// Guid value.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool operator ==(Id x, Id y)
        {
            if ((object)x == null)
            {
                return (object)y == null;
            }

            return x._guidVal == y._guidVal && x._strVal == y._strVal && x._intVal == y._intVal && x._longVal == y._longVal;
        }

        /// <summary>
        /// Determines if both Id have the same underlying of int
        /// Int value.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool operator <(Id x, Id y)
        {
            if (x._strVal != null && y._strVal != null)
            {
                return string.Compare(x._strVal, y._strVal, StringComparison.InvariantCulture) < 0;
            }
            else if (x._intVal.HasValue && y._intVal.HasValue)
            {
                return x._intVal.Value < y._intVal.Value;
            }
            else if (x._longVal.HasValue && y._longVal.HasValue)
            {
                return x._longVal.Value < y._longVal.Value;
            }
            throw new ArithmeticException("Cannot determine what type of underlaying Id data");
        }

        public static bool operator >(Id x, Id y)
        {
            if (x._strVal != null && y._strVal != null)
            {
                return string.Compare(x._strVal, y._strVal, StringComparison.InvariantCulture) > 0;
            }
            else if (x._intVal.HasValue && (y._intVal.HasValue || y._longVal.HasValue))
            {
                return y._intVal.HasValue ? x._intVal.Value >= y._intVal.Value : x._intVal.Value >= y._longVal.Value;
            }
            else if (x._longVal.HasValue && (y._intVal.HasValue || y._longVal.HasValue))
            {
                return y._intVal.HasValue ? x._longVal.Value >= y._intVal.Value : x._longVal.Value >= y._longVal.Value;
            }
            throw new ArithmeticException("Cannot determine what type of underlaying Id data");
        }

        /// <summary>
        /// Determines if both Id have the same underlying of int
        /// Int value.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool operator <=(Id x, Id y)
        {
            if (x._strVal != null && y._strVal != null)
            {
                return string.Compare(x._strVal, y._strVal, StringComparison.InvariantCulture) <= 0;
            }
            else if (x._intVal.HasValue && (y._intVal.HasValue || y._longVal.HasValue))
            {
                return y._intVal.HasValue ? x._intVal.Value <= y._intVal.Value : x._intVal.Value <= y._longVal.Value;
            }
            else if (x._longVal.HasValue && (y._intVal.HasValue || y._longVal.HasValue))
            {
                return y._intVal.HasValue ? x._longVal.Value <= y._intVal.Value : x._longVal.Value <= y._longVal.Value;
            }
            throw new ArithmeticException("Cannot determine what type of underlaying Id data");
        }

        public static bool operator >=(Id x, Id y)
        {
            if (x._strVal != null && y._strVal != null)
            {
                return string.Compare(x._strVal, y._strVal, StringComparison.InvariantCulture) >= 0;
            }
            else if (x._intVal.HasValue && (y._intVal.HasValue || y._longVal.HasValue))
            {
                return y._intVal.HasValue ? x._intVal.Value >= y._intVal.Value : x._intVal.Value >= y._longVal.Value;
            }
            else if (x._longVal.HasValue && (y._intVal.HasValue || y._longVal.HasValue))
            {
                return y._intVal.HasValue ? x._longVal.Value >= y._intVal.Value : x._longVal.Value >= y._longVal.Value;
            }
            throw new ArithmeticException("Cannot determine what type of underlaying Id data");
        }

        /// <summary>
        /// Determines if both ShortGuids do not have the 
        /// same underlying Guid value.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool operator !=(Id x, Id y)
        {
            return x._guidVal != y._guidVal && x._strVal != y._strVal && x._intVal != y._intVal && x._longVal != y._longVal;
        }

        /// <summary>
        /// Implicitly converts the ShortGuid to it's string equivilent
        /// </summary>
        /// <param name="shortGuid"></param>
        /// <returns></returns>
        public static implicit operator string(Id shortGuid)
        {
            return shortGuid._strVal;
        }
        public static implicit operator int(Id d) => d._intVal ?? 0;

        public static implicit operator long(Id id)
        {
            return id._longVal ?? 0;
        }

        /// <summary>
        /// Implicitly converts the ShortGuid to it's Guid equivilent
        /// </summary>
        /// <param name="shortGuid"></param>
        /// <returns></returns>
        public static implicit operator Guid(Id shortGuid)
        {
            return shortGuid._guidVal ?? Guid.Empty;
        }

        /// <summary>
        /// Implicitly converts the string to a ShortGuid
        /// </summary>
        /// <param name="shortGuid"></param>
        /// <returns></returns>
        public static implicit operator Id(string shortGuid)
        {
            return new Id(shortGuid);
        }

        /// <summary>
        /// Implicitly converts the Guid to a ShortGuid 
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static implicit operator Id(Guid guid)
        {
            return new Id(guid);
        }

        public static implicit operator Id(int i32)
        {
            return new Id(i32);
        }

        public static implicit operator Id(long i64)
        {
            return new Id(i64);
        }

        #endregion
    }
}