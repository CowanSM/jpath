using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JSONPathVS {

    public delegate void Tokener(string c);

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
            tokenDict.Add("[0-9\\.]", typeof(ConstructDouble));
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


    //public class JSONParser {
    //    [Flags]
    //    private enum ParseFlags {
    //        None = 0x0,
    //        BuildingString = 0x1,
    //        BuildingDouble = 0x2,
    //        BuildingBoolean = 0x4,
    //        BuildingArray = 0x8,
    //        BuildingObject = 0x10,
    //        InnerObject = 0x20
    //    }

    //    private static MemoryStream currentInput;
    //    private static JSONObject rootObject;

    //    private Dictionary<String, Tokener> tokenDictionary;
    //    private ParseFlags pFlags;
    //    private StringBuilder strBuilder;
    //    private string currKey;
    //    private JSONObject currObject, currArray;
    //    private int offset;

    //    static JSONParser() {
    //        currentInput = null;
    //        rootObject = null;
    //    }

    //    public JSONParser() {
    //        pFlags = ParseFlags.None;
    //        strBuilder = new StringBuilder();

    //        tokenDictionary = new Dictionary<string, Tokener>();
    //        tokenDictionary.Add("\\", ReadEscapeCharacter);
    //        tokenDictionary.Add("\"", ReadQuotation);
    //        tokenDictionary.Add("{", ReadOpenBracket);
    //        tokenDictionary.Add("}", ReadCloseBracket);
    //        tokenDictionary.Add("[", ReadOpenSquare);
    //        tokenDictionary.Add("]", ReadCloseSquare);
    //        tokenDictionary.Add(",", ReadComma);
    //        tokenDictionary.Add(":", ReadColon);
    //    }

    //    public static void SetCurrentStream(MemoryStream stream) {
    //        if (currentInput != null) {
    //            currentInput.Close();
    //            currentInput.Dispose();
    //        }

    //        currentInput = stream;
    //        rootObject = null;
    //    }

    //    public static JSONObject Root {
    //        get {
    //            if (rootObject == null) {
    //                JSONParser parser = new JSONParser();
    //                rootObject = parser.ParseInput();
    //            }
    //            return rootObject;
    //        }
    //    }

    //    public JSONObject ParseInput() {
    //        currObject = new JSONObject();
    //        currArray = new JSONObject();
    //        offset = 0;
    //        if (currentInput != null) {
    //            do {
    //                string c = ReadNextChar();
    //                TokenizeChar(c);
    //            } while ((pFlags & ParseFlags.BuildingObject) == ParseFlags.BuildingObject);
    //        }
    //        return currObject;
    //    }


    //    private string ReadNextChar() {
    //        byte[] nextByte = new byte[1];
    //        if (offset < currentInput.Length) {
    //            currentInput.Read(nextByte, 0, 1);
    //            offset++;
    //        }
    //        return System.Text.Encoding.ASCII.GetString(nextByte);
    //    }

    //    private void TokenizeChar(string c) {
    //        Tokener action;
    //        if (tokenDictionary.ContainsKey(c)) {
    //            action = tokenDictionary[c];
    //        } else {
    //            action = ReadAlphaNumeric;
    //        }
    //        action(c);
    //    }

    //    private void ReadEscapeCharacter(string c) {
    //        if ((pFlags & ParseFlags.BuildingString) == ParseFlags.BuildingString) {
    //            // add two characters to the string
    //            strBuilder.Append(c);
    //            c = ReadNextChar();
    //            strBuilder.Append(c);
    //        }
    //    }

    //    private void ReadAlphaNumeric(string c) {
    //        if ((pFlags & ParseFlags.BuildingString) == ParseFlags.BuildingString ||
    //            (pFlags & ParseFlags.BuildingDouble) == ParseFlags.BuildingDouble ||
    //            (pFlags & ParseFlags.BuildingBoolean) == ParseFlags.BuildingBoolean) {
    //            strBuilder.Append(c);
    //        } else if (System.Text.RegularExpressions.Regex.IsMatch(c,@"[\s]+")) {
    //            return;
    //        }else {
    //            if (c.ToLower().Equals("t") || c.ToLower().Equals("f")) {
    //                pFlags = pFlags | ParseFlags.BuildingBoolean;
    //                strBuilder.Append(c);
    //            } else {
    //                pFlags = pFlags | ParseFlags.BuildingDouble;
    //                strBuilder.Append(c);
    //            }
    //        }
    //    }

    //    private void ReadCloseBracket(string c) {
    //        if ((pFlags & ParseFlags.BuildingString) == ParseFlags.BuildingString) {
    //            strBuilder.Append(c);
    //        } else {
    //            pFlags = pFlags ^ ParseFlags.BuildingObject;
    //            // add child value, just in case
    //            this.AddChildValue();
    //        }
    //    }

    //    private void ReadOpenBracket(string c) {
    //        if ((pFlags & ParseFlags.BuildingObject) == ParseFlags.None) {
    //            pFlags = pFlags | ParseFlags.BuildingObject;
    //        } else if ((pFlags & ParseFlags.BuildingString) == ParseFlags.BuildingString) {
    //            strBuilder.Append(c);
    //        } else if ((pFlags & ParseFlags.BuildingArray) == ParseFlags.BuildingArray) {
    //            // parse inner object
    //            JSONParser inparse = new JSONParser();
    //            JSONObject innerobj = inparse.ParseInput();
    //            // add object to current array
    //            currArray.Add(innerobj);
    //        } else {
    //            // parse inner object
    //            JSONParser inparse = new JSONParser();
    //            inparse.pFlags = inparse.pFlags | ParseFlags.BuildingObject;
    //            JSONObject innerObject = inparse.ParseInput();
    //            // add object to current list
    //            currObject.AddChild(currKey, innerObject);
    //        }
    //    }

    //    private void ReadCloseSquare(string c) {
    //        if ((pFlags & ParseFlags.BuildingString) == ParseFlags.BuildingString) {
    //            strBuilder.Append(c);
    //        } else if ((pFlags & ParseFlags.BuildingArray) == ParseFlags.BuildingArray) {
    //            // add item to array
    //            this.AddArrayValue();
    //            // add array to object
    //            currObject.AddChild(currKey, currArray);
    //            pFlags = pFlags ^ ParseFlags.BuildingArray;
    //        }
    //    }

    //    private void ReadOpenSquare(string c) {
    //        if ((pFlags & ParseFlags.BuildingString) == ParseFlags.BuildingString) {
    //            strBuilder.Append(c);
    //        } else {
    //            pFlags = pFlags | ParseFlags.BuildingArray;
    //            this.currArray.InitializeArray();
    //        }
    //    }

    //    private void ReadQuotation(string c) {
    //        if ((pFlags & ParseFlags.BuildingString) == ParseFlags.None) {
    //            pFlags = pFlags | ParseFlags.BuildingString;
    //        } else {
    //            pFlags = pFlags ^ ParseFlags.BuildingString;
    //        }
    //    }

    //    private void ReadComma(string c) {
    //        if ((pFlags & ParseFlags.BuildingString) == ParseFlags.BuildingString) {
    //            strBuilder.Append(c);
    //        } else if ((pFlags & ParseFlags.BuildingArray) == ParseFlags.BuildingArray) {
    //            // add value to current array
    //            this.AddArrayValue();
    //        } else {
    //            // add value to object
    //            this.AddChildValue();
    //        }
    //    }

    //    private void ReadColon(string c) {
    //        // set key and continue
    //        currKey = strBuilder.ToString();
    //        strBuilder = new StringBuilder();
    //    }

    //    private void AddArrayValue() {
    //        if ((pFlags & ParseFlags.BuildingBoolean) == ParseFlags.BuildingBoolean) {
    //            bool b = Boolean.Parse(strBuilder.ToString());
    //            currArray.Add(new JSONObject(b));
    //            pFlags = pFlags ^ ParseFlags.BuildingBoolean;
    //        } else if ((pFlags & ParseFlags.BuildingDouble) == ParseFlags.BuildingDouble) {
    //            double d = Double.Parse(strBuilder.ToString());
    //            currArray.Add(new JSONObject(d));
    //            pFlags = pFlags ^ ParseFlags.BuildingDouble;
    //        } else {
    //            string s = strBuilder.ToString();
    //            currArray.Add(new JSONObject(s));
    //        }
    //        strBuilder = new StringBuilder();
    //    }

    //    private void AddChildValue() {
    //        if ((pFlags & ParseFlags.BuildingBoolean) == ParseFlags.BuildingBoolean) {
    //            bool b = Boolean.Parse(strBuilder.ToString());
    //            currObject.AddChild(currKey, new JSONObject(b));
    //            pFlags = pFlags ^ ParseFlags.BuildingBoolean;
    //        } else if ((pFlags & ParseFlags.BuildingDouble) == ParseFlags.BuildingDouble) {
    //            double d = Double.Parse(strBuilder.ToString());
    //            currObject.AddChild(currKey, new JSONObject(d));
    //            pFlags = pFlags ^ ParseFlags.BuildingDouble;
    //        } else if (!currObject.HasChild(currKey)) {
    //            string s = strBuilder.ToString();
    //            currObject.AddChild(currKey, new JSONObject(s));
    //        }
    //        strBuilder = new StringBuilder();
    //    }
    //}

}
