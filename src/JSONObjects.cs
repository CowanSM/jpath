using System;
using System.Collections.Generic;

namespace JSONPathVS {
    public class JSONObject {
        private Dictionary<string, JSONObject> children;
        private List<JSONObject> array;
        private string svalue;
        private double dvalue;
        private bool bvalue;

        internal enum JsonType { jobj, jarr, jnum, jbool, jstr, jnull };
        internal JsonType type;

        public JSONObject() {
            type = JsonType.jobj;
            children = new Dictionary<string, JSONObject>();
            array = null;
            svalue = null;
            dvalue = double.NaN;
        }

        public JSONObject(double d) {
            type = JsonType.jnum;
            children = null;
            array = null;
            svalue = null;
            dvalue = d;
        }

        public JSONObject(string s) {
            type = JsonType.jstr;
            dvalue = double.NaN;
            children = null;
            array = null;
            svalue = s;
        }

        public JSONObject(bool b) {
            type = JsonType.jbool;
            children = null;
            array = null;
            bvalue = b;
            dvalue = double.NaN;
        }

        public JSONObject this[string key] {
            get {
                if (children != null && children.ContainsKey(key)) {
                    return children[key];
                }
                return null;
            }
        }

        public JSONObject this[int key] {
            get {
                if (array != null && key < array.Count) {
                    return array[key];
                }
                return null;
            }
        }

        #region operators
        public static implicit operator double(JSONObject jo) {
            return jo.dvalue;
        }

        public static implicit operator string(JSONObject jo) {
            return jo.svalue;
        }

        public static implicit operator bool(JSONObject jo) {
            return jo.bvalue;
        }

        public static implicit operator JSONObject(double d) {
            return new JSONObject(d);
        }

        public static implicit operator JSONObject(string s) {
            return new JSONObject(s);
        }

        public static implicit operator JSONObject(bool b) {
            return new JSONObject(b);
        }

        public override bool Equals(object obj) {
            if (obj.GetType() == typeof(string)) {
                return obj.ToString().Equals(this.svalue);
            } else if (obj.GetType() == typeof(double)) {
                return (double)obj == this.dvalue;
            } else if (obj.GetType() == typeof(bool)) {
                return (bool)obj == this.bvalue;
            } else if (obj.GetType() == typeof(JSONObject)) {
                JSONObject jo = (JSONObject)obj;
                if (this.type != jo.type)
                    return false;

                switch (jo.type) {
                    case JsonType.jarr:
                        return this.Equals(jo.array);
                    case JsonType.jbool:
                        return this.Equals(jo.bvalue);
                    case JsonType.jnum:
                        return this.Equals(jo.dvalue);
                    case JsonType.jobj:
                        bool ret = (jo.children.Count == this.children.Count);
                        foreach (var child in jo.children) {
                            if (!this.children.ContainsKey(child.Key))
                                ret = false;
                        }
                        return ret;
                    case JsonType.jstr:
                        return this.Equals(jo.svalue);
                }
            } else if (obj.GetType() == typeof(List<JSONObject>)) {
                return this.array == (List<JSONObject>)obj;
            }

            return false;
        }

        #region bool operators

        public static bool operator ==(JSONObject jo, bool b) {
            return jo.bvalue == b;
        }

        public static bool operator !=(JSONObject jo, bool b) {
            return jo.bvalue != b;
        }

        #endregion

        #region string operators

        public static bool operator ==(JSONObject jo, object s) {
            if (System.Object.ReferenceEquals(jo, null) && System.Object.ReferenceEquals(s,null)) {
                return true;
            } else if (System.Object.ReferenceEquals(s,null) || System.Object.ReferenceEquals(jo,null)) {
                return false;
            } else if (s.GetType() == typeof(string)) {
                return s.ToString().Equals(jo.svalue);
            }

            return false;
        }

        public static bool operator !=(JSONObject jo, object s) {
            if (System.Object.ReferenceEquals(jo, null) && System.Object.ReferenceEquals(s, null)) {
                return false;
            } else if (System.Object.ReferenceEquals(s, null) || System.Object.ReferenceEquals(jo,null)) {
                return true;
            } else if (s.GetType() == typeof(string)) {
                return !s.ToString().Equals(jo.svalue);
            }

            return true;
        }

        public static string operator +(JSONObject jo, string s) {
            if (System.Object.ReferenceEquals(jo, null) || jo.svalue == null)
                return s;
            return jo.svalue + s;
        }

        public static string operator +(string s, JSONObject jo) {
            if (System.Object.ReferenceEquals(jo, null) || jo.svalue == null)
                return s;
            return s + jo.svalue;
        }

        #endregion

        #region double operators

        public static bool operator !=(JSONObject jo, double d) {
            return jo.dvalue != d;
        }

        public static bool operator ==(JSONObject jo, double d) {
            return jo.dvalue == d;
        }

        public static double operator +(JSONObject jo, double d) {
            return jo.dvalue + d;
        }

        public static double operator +(double d, JSONObject jo) {
            return jo.dvalue + d;
        }

        public static double operator -(JSONObject jo, double d) {
            return jo.dvalue - d;
        }

        public static double operator -(double d, JSONObject jo) {
            return d - jo.dvalue;
        }

        public static double operator *(JSONObject jo, double d) {
            return jo.dvalue * d;
        }

        public static double operator *(double d, JSONObject jo) {
            return jo.dvalue * d;
        }

        public static double operator /(JSONObject jo, double d) {
            return jo.dvalue / d;
        }

        public static double operator /(double d, JSONObject jo) {
            return d / jo.dvalue;
        }

        #endregion

        #endregion

        public override string ToString() {
            switch (this.type) {
                case JsonType.jarr:
                    string retstr = "[";
                    foreach (var obj in this.array) {
                        retstr += obj.ToString() + ", ";
                    }
                    retstr = retstr.Substring(0, retstr.Length - 2);
                    return retstr + "]";
                case JsonType.jbool:
                    if (this.bvalue)
                        return "true";
                    else
                        return "false";
                case JsonType.jnum:
                    return this.dvalue.ToString();
                case JsonType.jobj:
                    string str = "{";
                    if (this.children.Count > 0) {
                        foreach (var obj in this.children) {
                            str += "\"" + obj.Key + "\":" + obj.Value.ToString() + ", ";
                        }
                        str = str.Substring(0, str.Length - 2);
                    }
                    return str + "}";
                case JsonType.jstr:
                    return "\"" + this.svalue + "\"";
                case JsonType.jnull:
                    return "null";
                default:
                    return "unknown type";
            }
        }

        public void InitializeArray() {
            array = new List<JSONObject>();
            children = null;
            svalue = null;
            dvalue = double.NaN;
            type = JsonType.jarr;
        }

        public void SetAsNull() {
            array = null;
            children = null;
            svalue = null;
            this.type = JsonType.jnull;
        }

        public bool HasChild(string key) {
            if (this.type == JsonType.jobj && this.children.ContainsKey(key)) {
                return true;
            }
            return false;
        }

        public void AddChild(string key, JSONObject child) {
            if (this.type == JsonType.jobj) {
                this.children.Add(key, child);
            }
        }

        public void Add(JSONObject child) {
            if (this.type == JsonType.jarr) {
                this.array.Add(child);
            }
        }
    }

    //public abstract class JSONBase {
    //    protected Dictionary<string, JSONBase> children;
    //    protected List<JSONBase> array;

    //    public virtual JSONBase this[string key] {
    //        get {
    //            if (children != null && children.ContainsKey(key)) {
    //                return children[key];
    //            }
    //            return null;
    //        }
    //        set {
    //            if (children != null) {
    //                if (children.ContainsKey(key)) {
    //                    children[key] = value;
    //                } else {
    //                    children.Add(key, value);
    //                }
    //            }
    //        }
    //    }

    //    public virtual JSONBase this[int i] {
    //        get {
    //            if (array != null && i < array.Count) {
    //                return array[i];
    //            }
    //            return null;
    //        }
    //        set {
    //            if (array != null && i < array.Count) {
    //                array[i] = value;
    //            }
    //        }
    //    }

    //    public virtual void AddItem(JSONBase item) {
    //        if (array != null)
    //            array.Add(item);
    //    }
    //}

    //public class JSONObject : JSONBase {
    //    public JSONObject() {
    //        this.children = new Dictionary<string, JSONBase>();
    //    }

    //    public void AddChild(string key, JSONBase obj) {
    //        this.children.Add(key, obj);
    //    }
    //}

    //public class JSONArray : JSONBase {
    //    public JSONArray() {
    //        this.array = new List<JSONBase>();
    //    }

    //    public void Add(JSONBase jb) {
    //        this.array.Add(jb);
    //    }
    //}

    //public class JSONBool : JSONBase {
    //    private bool bvalue;

    //    public JSONBool(bool b) {
    //        this.bvalue = b;
    //    }

    //    public static implicit operator JSONBool(bool b) {
    //        return new JSONBool(b);
    //    }

    //    public static implicit operator bool(JSONBool jb) {
    //        return jb.bvalue;
    //    }

    //    public static bool operator !=(JSONBool jb, bool b) {
    //        return jb.bvalue != b;
    //    }

    //    public static bool operator ==(JSONBool jb, bool b) {
    //        return jb.bvalue == b;
    //    }
    //}

    //public class JSONString : JSONBase {
    //    private string svalue;

    //    public JSONString(string s) {
    //        this.svalue = s;
    //    }

    //    public static implicit operator JSONString(string s) {
    //        return new JSONString(s);
    //    }

    //    public static implicit operator string(JSONString js) {
    //        return js.svalue;
    //    }

    //    public static bool operator !=(JSONString js, string s) {
    //        return !js.svalue.Equals(s);
    //    }

    //    public static bool operator ==(JSONString js, string s) {
    //        return js.svalue.Equals(s);
    //    }
    //}

    //public class JSONNumber : JSONBase {
    //    private double dvalue;

    //    public JSONNumber(double d) {
    //        this.dvalue = d;
    //    }

    //    public static implicit operator JSONNumber(double d) {
    //        return new JSONNumber(d);
    //    }

    //    public static implicit operator double(JSONNumber jn) {
    //        return jn.dvalue;
    //    }

    //    public static bool operator ==(JSONNumber jn, double d) {
    //        return jn.dvalue == d;
    //    }

    //    public static bool operator ==(double d, JSONNumber jn) {
    //        return jn.dvalue == d;
    //    }

    //    public static bool operator !=(JSONNumber jn, double d) {
    //        return jn.dvalue != d;
    //    }

    //    public static bool operator !=(double d, JSONNumber jn) {
    //        return jn.dvalue != d;

    //    }

    //    public static bool operator <(JSONNumber jn, double d) {
    //        return jn.dvalue < d;
    //    }

    //    public static bool operator >(JSONNumber jn, double d) {
    //        return jn.dvalue > d;
    //    }

    //    public static JSONNumber operator +(JSONNumber jn, double d) {
    //        return new JSONNumber(jn.dvalue + d);
    //    }

    //    public static JSONNumber operator *(JSONNumber jn, double d) {
    //        return new JSONNumber(jn.dvalue * d);
    //    }

    //    public static JSONNumber operator -(JSONNumber jn, double d) {
    //        return new JSONNumber(jn.dvalue - d);
    //    }

    //    public static JSONNumber operator -(double d, JSONNumber jn) {
    //        return new JSONNumber(d - jn.dvalue);
    //    }

    //    public static JSONNumber operator /(JSONNumber jn, double d) {
    //        return new JSONNumber(jn.dvalue / d);
    //    }

    //    public static JSONNumber operator /(double d, JSONNumber jn) {
    //        return new JSONNumber(d / jn.dvalue);
    //    }
    //}
}
