    using System;
    using System.IO;
    using System.Text;
    using System.Xml.Serialization;

    public static class Serializer
    {
        public static T FromXML<T>(this string str) where T : class
        {
            if (string.IsNullOrEmpty(str)) throw new ArgumentNullException("str");

            XmlSerializer xs = new XmlSerializer(typeof(T));
            using (StringReader sr = new StringReader(str))
            {
                object obj = xs.Deserialize(sr);
                sr.Close();
                return obj as T;
            }
        }
        
        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }
    
        /// <summary>
        /// object转XML 多用于简单类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToXML<T>(this T obj) where T : class
        {
            if (obj == null) throw new ArgumentNullException("obj");

            XmlSerializer xs = new XmlSerializer(typeof(T));
            using (Utf8StringWriter sw = new Utf8StringWriter())
            {
                xs.Serialize(sw, obj);
                string xml = sw.ToString();
                sw.Close();
                return xml;
            }
        }
    }