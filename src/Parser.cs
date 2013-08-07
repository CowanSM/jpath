using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jpath {

    public interface IJsonConstruct {
        JSONObject DoConstruction(string ch, JSONParser parser);
        void AddObject(JSONObject obj);
    }

    public class ConstructObject : IJsonConstruct {
        private string currentKey;
        private JSONObject objectBuilding;

        public ConstructObject() {
            currentKey = null;
            objectBuilding = new JSONObject();
        }

        #region IJsonConstruct Members

        public JSONObject DoConstruction(string ch, JSONParser parser) {
            // check the tokenKey dict
            foreach (string keyPattern in JSONParser.tokenDict.Keys) {
                if (System.Text.RegularExpressions.Regex.IsMatch(ch, keyPattern)) {
                    IJsonConstruct nextAction;
                    if (JSONParser.tokenDict[keyPattern] == typeof(ConstructBoolean) || JSONParser.tokenDict[keyPattern] == typeof(ConstructDouble)) {
                        nextAction = (IJsonConstruct)JSONParser.tokenDict[keyPattern].GetConstructor(new Type[] { typeof(string) }).Invoke(new object[] { ch });
                    } else {
                        nextAction = (IJsonConstruct)JSONParser.tokenDict[keyPattern].GetConstructor(new Type[] { }).Invoke(null);
                    }
                    parser.AddToStack(nextAction);
                    return null;
                }
            }

            // ignore whitespace
            if (System.Text.RegularExpressions.Regex.IsMatch(ch, @"\s+")) {
                return null;
            }

            if (ch.Equals("}")) {
                // done, return the object we were building
                return objectBuilding;
            } else if (ch.Equals(",") || ch.Equals(":")) {
                // ignore the ',' and ':' characters
                return null;
            } else {
                throw new ArgumentException("Unexpected character while building object: " + ch);
            }

            // continue, return null
            return null;
        }

        public void AddObject(JSONObject obj) {
            if (currentKey == null) {
                currentKey = obj;
            } else {
                objectBuilding.AddChild(currentKey, obj);
                currentKey = null;
            }
        }

        #endregion
    }

    public class ConstructArray : IJsonConstruct {
        private JSONObject objectBuilding;

        public ConstructArray() {
            objectBuilding = new JSONObject();
            objectBuilding.InitializeArray();
        }

        #region IJsonConstruct Members

        public JSONObject DoConstruction(string ch, JSONParser parser) {
            foreach (string keyPattern in JSONParser.tokenDict.Keys) {
                if (System.Text.RegularExpressions.Regex.IsMatch(ch, keyPattern)) {
                    IJsonConstruct nextAction;
                    if (JSONParser.tokenDict[keyPattern] == typeof(ConstructBoolean) || JSONParser.tokenDict[keyPattern] == typeof(ConstructDouble)) {
                        nextAction = (IJsonConstruct)JSONParser.tokenDict[keyPattern].GetConstructor(new Type[] { typeof(string) }).Invoke(new object[] { ch });
                    } else {
                        nextAction = (IJsonConstruct)JSONParser.tokenDict[keyPattern].GetConstructor(new Type[] { }).Invoke(null);
                    }
                    parser.AddToStack(nextAction);
                    return null;
                }
            }

            // ignore whitespace
            if (System.Text.RegularExpressions.Regex.IsMatch(ch, @"\s+")) {
                return null;
            }

            if (ch.Equals("]")) {
                return objectBuilding;
            } else if (ch.Equals(",")) {
                return null;
            } else {
                throw new ArgumentException("Unexpected character while building object: " + ch);
            }

            return null;
        }

        public void AddObject(JSONObject obj) {
            objectBuilding.Add(obj);
        }

        #endregion
    }

    public class ConstructString : IJsonConstruct {
        private JSONObject objectBuilding;
        private StringBuilder strBuilder;

        public ConstructString() {
            objectBuilding = new JSONObject();
            strBuilder = new StringBuilder();
        }

        #region IJsonConstruct Members

        public JSONObject DoConstruction(string ch, JSONParser parser) {
            if (ch.Equals("\"")) {
                // finished string
                objectBuilding = strBuilder.ToString();
                return objectBuilding;
            } else {
                strBuilder.Append(ch);
            }
            return null;
        }

        public void AddObject(JSONObject obj) {
            throw new InvalidOperationException("Objects cannot be added to ConstructString construct");
        }

        #endregion
    }

    public class ConstructBoolean : IJsonConstruct {
        private JSONObject objBuilding;
        private StringBuilder strBuilder;

        public ConstructBoolean(string firstCharacter) {
            objBuilding = new JSONObject();
            strBuilder = new StringBuilder();
            strBuilder.Append(firstCharacter);
        }

        #region IJsonConstruct Members

        public JSONObject DoConstruction(string ch, JSONParser parser) {
            if (ch.Equals(",") || ch.Equals("]") || ch.Equals("}")) {
                // return object
                objBuilding = Boolean.Parse(strBuilder.ToString());
                return objBuilding;
            } else {
                strBuilder.Append(ch);
            }
            return null;
        }

        public void AddObject(JSONObject obj) {
            throw new InvalidOperationException("Objects cannot be added to ConstructBoolean construct");
        }

        #endregion
    }

    public class ConstructDouble : IJsonConstruct {
        private JSONObject objBuilding;
        private StringBuilder strBuilder;

        public ConstructDouble(string firstCharacter) {
            objBuilding = new JSONObject();
            strBuilder = new StringBuilder();
            strBuilder.Append(firstCharacter);
        }

        #region IJsonConstruct Members

        public JSONObject DoConstruction(string ch, JSONParser parser) {
            if (ch.Equals(",") || ch.Equals("]") || ch.Equals("}")) {
                // return object
                objBuilding = Double.Parse(strBuilder.ToString());
                return objBuilding;
            } else {
                strBuilder.Append(ch);
            }
            return null;
        }

        public void AddObject(JSONObject obj) {
            throw new InvalidOperationException("Objects cannot be added to ConstructDouble constructs");
        }

        #endregion
    }

    public class ConstructNull : IJsonConstruct {
        private JSONObject objBuidling;

        public ConstructNull() {
            objBuidling = new JSONObject();
            objBuidling.SetAsNull();
        }

        #region IJsonConstruct Members

        public JSONObject DoConstruction(string ch, JSONParser parser) {
            if (ch.Equals(",") || ch.Equals("]") || ch.Equals("}")) {
                return objBuidling;
            }
            return null;
        }

        public void AddObject(JSONObject obj) {
            throw new InvalidOperationException("Cannot add objects to ConstructNull construct");
        }

        #endregion
    }

    public class JSONParser {

        internal static Dictionary<string, Type> tokenDict;

        private Stream inputStream;
        private Stack<IJsonConstruct> constructStack;

        static JSONParser() {
            tokenDict = new Dictionary<string, Type>();
            tokenDict.Add("\\{", typeof(ConstructObject));
            tokenDict.Add("\\[", typeof(ConstructArray));
            tokenDict.Add("\\\"", typeof(ConstructString));
            tokenDict.Add("[TtFf]", typeof(ConstructBoolean));
            tokenDict.Add("[\\-0-9\\.]", typeof(ConstructDouble));
            tokenDict.Add("[nN]", typeof(ConstructNull));
        }

        public static JSONParser CreateParser(Stream inputStream) {
            return new JSONParser(inputStream);
        }

        public JSONParser(Stream inputStream) {
            // TODO: Complete member initialization
            this.inputStream = inputStream;
            this.constructStack = new Stack<IJsonConstruct>();
        }

        public JSONObject Root {
            get {
                return this.GetRoot();
            }
        }

        private JSONObject CheckCharacter(string ch) {
            JSONObject ret = this.constructStack.Peek().DoConstruction(ch, this);
            if (ret != null) {
                this.constructStack.Pop();
                if (this.constructStack.Count > 0) {
                    this.constructStack.Peek().AddObject(ret);
                } else {
                    return ret;
                }
            }
            return ret;
        }

        private JSONObject GetRoot() {
            int byteRead;
            byte[] buffer = new byte[1];
            // read the first byte make sure it is an '{'
            byteRead = inputStream.ReadByte();
            buffer[0] = (byte)byteRead;
            if (System.Text.Encoding.ASCII.GetString(buffer).Equals("{")) {
                this.constructStack.Push((IJsonConstruct)new ConstructObject());
            } else {
                throw new ArgumentException("First character of JSON string was not '{', therefore invalid JSON string!");
            }

            // read subsequent bytes until EoS
            while ((byteRead = inputStream.ReadByte()) > 0) {
                buffer[0] = (byte)byteRead;
                string c = System.Text.Encoding.ASCII.GetString(buffer);
                JSONObject ret = CheckCharacter(c);
                // pass on '}' or ']' to the parent object
                if (c.Equals("]") && ret.type != JSONObject.JsonType.jarr) {
                    ret = CheckCharacter(c);
                } else if (c.Equals("}") && ret.type != JSONObject.JsonType.jobj) {
                    ret = CheckCharacter(c);
                }

                if (this.constructStack.Count == 0) {
                    return ret;
                }
            }
            // didn't parse correctly, return null
            return null;
        }

        internal void PopTopStack() {
            if (this.constructStack.Count > 0)
                this.constructStack.Pop();
        }

        internal void AddToStack(IJsonConstruct toAdd) {
            this.constructStack.Push(toAdd);
        }

        public static JSONObject ParseJsonString(Stream inputStream) {
            JSONParser parser = new JSONParser(inputStream);
            return parser.Root;
        }
    }

}
