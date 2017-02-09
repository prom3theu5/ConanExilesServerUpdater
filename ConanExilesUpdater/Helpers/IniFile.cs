using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ConanExilesUpdater
{
    public class INIFile
    {

        #region "Declarations"

        private object m_Lock = new object();

        private string m_FileName = null;
        internal string FileName
        {
            get
            {
                return m_FileName;
            }
        }

        private bool m_Lazy = false;

        private bool m_AutoFlush = false;

        private Dictionary<string, Dictionary<string, string>> m_Sections = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, Dictionary<string, string>> m_Modified = new Dictionary<string, Dictionary<string, string>>();

        private bool m_CacheModified = false;

        #endregion

        #region "Methods"

        public INIFile(string FileName)
        {
            Initialize(FileName, false, false);
        }

        public INIFile(string FileName, bool Lazy, bool AutoFlush)
        {
            Initialize(FileName, Lazy, AutoFlush);
        }

        private void Initialize(string FileName, bool Lazy, bool AutoFlush)
        {
            m_FileName = FileName;
            m_Lazy = Lazy;
            m_AutoFlush = AutoFlush;
            if (!m_Lazy) Refresh();
        }

        private string ParseSectionName(string Line)
        {
            if (!Line.StartsWith("[")) return null;
            if (!Line.EndsWith("]")) return null;
            if (Line.Length < 3) return null;
            return Line.Substring(1, Line.Length - 2);
        }

        private bool ParseKeyValuePair(string Line, ref string Key, ref string Value)
        {
            int i;
            if ((i = Line.IndexOf('=')) <= 0) return false;

            int j = Line.Length - i - 1;
            Key = Line.Substring(0, i).Trim();
            if (Key.Length <= 0) return false;

            Value = (j > 0) ? (Line.Substring(i + 1, j).Trim()) : ("");
            return true;
        }
        internal void Refresh()
        {
            lock (m_Lock)
            {
                StreamReader sr = null;
                try
                {
                    m_Sections.Clear();
                    m_Modified.Clear();

                    try
                    {
                        sr = new StreamReader(m_FileName);
                    }
                    catch (FileNotFoundException)
                    {
                        return;
                    }

                    Dictionary<string, string> CurrentSection = null;
                    string s;
                    string SectionName;
                    string Key = null;
                    string Value = null;
                    while ((s = sr.ReadLine()) != null)
                    {
                        s = s.Trim();

                        SectionName = ParseSectionName(s);
                        if (SectionName != null)
                        {
                            if (m_Sections.ContainsKey(SectionName))
                            {
                                CurrentSection = null;
                            }
                            else
                            {
                                CurrentSection = new Dictionary<string, string>();
                                m_Sections.Add(SectionName, CurrentSection);
                            }
                        }
                        else if (CurrentSection != null)
                        {
                            if (ParseKeyValuePair(s, ref Key, ref Value))
                            {
                                if (!CurrentSection.ContainsKey(Key))
                                {
                                    CurrentSection.Add(Key, Value);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (sr != null) sr.Close();
                    sr = null;
                }
            }
        }

        internal void Flush()
        {
            lock (m_Lock)
            {
                PerformFlush();
            }
        }

        private void PerformFlush()
        {
            if (!m_CacheModified) return;
            m_CacheModified = false;

            bool OriginalFileExists = File.Exists(m_FileName);

            string TmpFileName = Path.ChangeExtension(m_FileName, "$n$");

            StreamWriter sw = null;

            sw = new StreamWriter(TmpFileName);

            try
            {
                Dictionary<string, string> CurrentSection = null;
                if (OriginalFileExists)
                {
                    StreamReader sr = null;
                    try
                    {
                        sr = new StreamReader(m_FileName);

                        string s;
                        string SectionName;
                        string Key = null;
                        string Value = null;
                        bool Unmodified;
                        bool Reading = true;
                        while (Reading)
                        {
                            s = sr.ReadLine();
                            Reading = (s != null);

                            if (Reading)
                            {
                                Unmodified = true;
                                s = s.Trim();
                                SectionName = ParseSectionName(s);
                            }
                            else
                            {
                                Unmodified = false;
                                SectionName = null;
                            }
                            if ((SectionName != null) || (!Reading))
                            {
                                if (CurrentSection != null)
                                {
                                    if (CurrentSection.Count > 0)
                                    {
                                        foreach (string fkey in CurrentSection.Keys)
                                        {
                                            if (CurrentSection.TryGetValue(fkey, out Value))
                                            {
                                                sw.Write(fkey);
                                                sw.Write('=');
                                                sw.WriteLine(Value);
                                            }
                                        }
                                        sw.WriteLine();
                                        CurrentSection.Clear();
                                    }
                                }

                                if (Reading)
                                {
                                    if (!m_Modified.TryGetValue(SectionName, out CurrentSection))
                                    {
                                        CurrentSection = null;
                                    }
                                }
                            }
                            else if (CurrentSection != null)
                            {
                                if (ParseKeyValuePair(s, ref Key, ref Value))
                                {
                                    if (CurrentSection.TryGetValue(Key, out Value))
                                    {
                                        Unmodified = false;
                                        CurrentSection.Remove(Key);

                                        sw.Write(Key);
                                        sw.Write('=');
                                        sw.WriteLine(Value);
                                    }
                                }
                            }

                            if (Unmodified)
                            {
                                sw.WriteLine(s);
                            }
                        }

                        sr.Close();
                        sr = null;
                    }
                    finally
                    {
                        if (sr != null) sr.Close();
                        sr = null;
                    }
                }

                foreach (KeyValuePair<string, Dictionary<string, string>> SectionPair in m_Modified)
                {
                    CurrentSection = SectionPair.Value;
                    if (CurrentSection.Count > 0)
                    {
                        sw.WriteLine();

                        sw.Write('[');
                        sw.Write(SectionPair.Key);
                        sw.WriteLine(']');

                        foreach (KeyValuePair<string, string> ValuePair in CurrentSection)
                        {
                            sw.Write(ValuePair.Key);
                            sw.Write('=');
                            sw.WriteLine(ValuePair.Value);
                        }
                        CurrentSection.Clear();
                    }
                }
                m_Modified.Clear();

                sw.Close();
                sw = null;

                File.Copy(TmpFileName, m_FileName, true);

                File.Delete(TmpFileName);
            }
            finally
            {
                if (sw != null) sw.Close();
                sw = null;
            }
        }

        internal string GetValue(string SectionName, string Key, string DefaultValue)
        {
            if (m_Lazy)
            {
                m_Lazy = false;
                Refresh();
            }

            lock (m_Lock)
            {
                Dictionary<string, string> Section;
                if (!m_Sections.TryGetValue(SectionName, out Section)) return DefaultValue;

                string Value;
                if (!Section.TryGetValue(Key, out Value)) return DefaultValue;

                return Value;
            }
        }

        internal void SetValue(string SectionName, string Key, string Value)
        {
            if (m_Lazy)
            {
                m_Lazy = false;
                Refresh();
            }

            lock (m_Lock)
            {
                m_CacheModified = true;

                Dictionary<string, string> Section;
                if (!m_Sections.TryGetValue(SectionName, out Section))
                {
                    Section = new Dictionary<string, string>();
                    m_Sections.Add(SectionName, Section);
                }

                if (Section.ContainsKey(Key)) Section.Remove(Key);
                Section.Add(Key, Value);
                if (!m_Modified.TryGetValue(SectionName, out Section))
                {
                    Section = new Dictionary<string, string>();
                    m_Modified.Add(SectionName, Section);
                }

                if (Section.ContainsKey(Key)) Section.Remove(Key);
                Section.Add(Key, Value);
                
                if (m_AutoFlush) PerformFlush();
            }
        }

        private string EncodeByteArray(byte[] Value)
        {
            if (Value == null) return null;

            StringBuilder sb = new StringBuilder();
            foreach (byte b in Value)
            {
                string hex = Convert.ToString(b, 16);
                int l = hex.Length;
                if (l > 2)
                {
                    sb.Append(hex.Substring(l - 2, 2));
                }
                else
                {
                    if (l < 2) sb.Append("0");
                    sb.Append(hex);
                }
            }
            return sb.ToString();
        }

        private byte[] DecodeByteArray(string Value)
        {
            if (Value == null) return null;

            int l = Value.Length;
            if (l < 2) return new byte[] { };

            l /= 2;
            byte[] Result = new byte[l];
            for (int i = 0; i < l; i++) Result[i] = Convert.ToByte(Value.Substring(i * 2, 2), 16);
            return Result;
        }

        internal bool GetValue(string SectionName, string Key, bool DefaultValue)
        {
            string StringValue = GetValue(SectionName, Key, DefaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            int Value;
            if (int.TryParse(StringValue, out Value)) return (Value != 0);
            return DefaultValue;
        }

        internal int GetValue(string SectionName, string Key, int DefaultValue)
        {
            string StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
            int Value;
            if (int.TryParse(StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out Value)) return Value;
            return DefaultValue;
        }

        internal long GetValue(string SectionName, string Key, long DefaultValue)
        {
            string StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
            long Value;
            if (long.TryParse(StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out Value)) return Value;
            return DefaultValue;
        }

        internal double GetValue(string SectionName, string Key, double DefaultValue)
        {
            string StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
            double Value;
            if (double.TryParse(StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out Value)) return Value;
            return DefaultValue;
        }

        internal byte[] GetValue(string SectionName, string Key, byte[] DefaultValue)
        {
            string StringValue = GetValue(SectionName, Key, EncodeByteArray(DefaultValue));
            try
            {
                return DecodeByteArray(StringValue);
            }
            catch (FormatException)
            {
                return DefaultValue;
            }
        }

        internal DateTime GetValue(string SectionName, string Key, DateTime DefaultValue)
        {
            string StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
            DateTime Value;
            if (DateTime.TryParse(StringValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.AssumeLocal, out Value)) return Value;
            return DefaultValue;
        }

        internal void SetValue(string SectionName, string Key, bool Value)
        {
            SetValue(SectionName, Key, (Value) ? ("1") : ("0"));
        }

        internal void SetValue(string SectionName, string Key, int Value)
        {
            SetValue(SectionName, Key, Value.ToString(CultureInfo.InvariantCulture));
        }

        internal void SetValue(string SectionName, string Key, long Value)
        {
            SetValue(SectionName, Key, Value.ToString(CultureInfo.InvariantCulture));
        }

        internal void SetValue(string SectionName, string Key, double Value)
        {
            SetValue(SectionName, Key, Value.ToString(CultureInfo.InvariantCulture));
        }

        internal void SetValue(string SectionName, string Key, byte[] Value)
        {
            SetValue(SectionName, Key, EncodeByteArray(Value));
        }

        internal void SetValue(string SectionName, string Key, DateTime Value)
        {
            SetValue(SectionName, Key, Value.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

    }

}