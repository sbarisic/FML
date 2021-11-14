using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FishMarkupLanguage {
    public class FMLTag {
        public string TagName;
        public FMLAttributes Attributes;

        public FMLTag Parent;
        public List<FMLTag> Children;

        public FMLTag() {
            Attributes = new FMLAttributes();
            Children = new List<FMLTag>();
            Parent = null;
        }

        public FMLTag(string Name) : this() {
            TagName = Name;
        }

        public virtual void AddChild(FMLTag T) {
            if (T.Parent != null)
                T.Parent.RemoveChild(T);

            T.Parent = this;
            Children.Add(T);
        }

        public virtual void RemoveChild(FMLTag T) {
            if (T.Parent == this)
                T.Parent = null;

            if (Children.Contains(T))
                Children.Remove(T);
        }

        public virtual void BuildString(int IndentLvl, StringBuilder SB) {
            if (IndentLvl > 0)
                SB.Append(new string('\t', IndentLvl));

            SB.Append(TagName);

            foreach (var A in Attributes.ToArray()) {
                SB.AppendFormat(" {0} = {1}", A.Key, FMLValueTag.ConvertToString(A.Value));
            }

            if (Children.Count > 0) {
                SB.AppendLine(" {");

                foreach (var Child in Children) {
                    Child.BuildString(IndentLvl + 1, SB);
                }

                if (IndentLvl > 0)
                    SB.Append(new string('\t', IndentLvl));

                SB.AppendLine("}").ToString();
            } else
                SB.AppendLine(";");
        }

        public override string ToString() {
            string Body = ";";

            if (Children.Count > 0) {
                Body = string.Format(" {{ {0} }}", string.Join(" ", Children));
            }

            return string.Format("{0}{1}{2}", TagName, Attributes.Count > 0 ? " " + Attributes.ToString() : "", Body);
        }

        public virtual XmlElement ToXmlElement(XmlDocument Doc) {
            XmlElement Element = Doc.CreateElement(TagName);

            foreach (var KV in Attributes.ToArray()) {
                Element.SetAttribute(KV.Key, KV.Value.ToString());
            }

            foreach (FMLTag C in Children) {
                if (C is FMLValueTag ValC) {
                    Element.InnerXml += ValC.Value.ToString();

                } else
                    Element.AppendChild(C.ToXmlElement(Doc));
            }

            return Element;
        }

        public virtual FMLTag ConstructFromTemplate(FMLTemplateTag Template, FMLTag TemplateInvoke) {
            FMLTag NewTag = new FMLTag(TagName);
            KeyValuePair<string, object>[] Attrs = Attributes.ToArray();

            for (int i = 0; i < Attrs.Length; i++) {
                object Value = Attrs[i].Value;

                if (Value is FMLTemplateValue TV) {
                    Value = TemplateInvoke.Attributes.GetAttribute(TV.Name);

                    if (Value == null)
                        Value = Template.Attributes.GetAttribute(TV.Name);
                }

                NewTag.Attributes.SetAttribute(Attrs[i].Key, Value);
            }

            for (int i = 0; i < Children.Count; i++)
                NewTag.AddChild(Children[i].ConstructFromTemplate(Template, TemplateInvoke));

            return NewTag;
        }
    }

    public class FMLTemplateTag : FMLTag {
        public string TemplateName;

        public FMLTemplateTag() : base() {
        }

        public override XmlElement ToXmlElement(XmlDocument Doc) {
            return null;
        }

        public FMLTag[] ConstructTags(FMLTag TemplateInvoke) {
            List<FMLTag> NewTags = new List<FMLTag>();

            for (int i = 0; i < Children.Count; i++) {
                /*FMLAttributes ChildAttrs = Children[i].Attributes;

                foreach (var KV in Attributes.ToArray()) {
                    if (ChildAttrs.GetAttribute(KV.Key) is FMLTemplateValue) {
                        ChildAttrs.SetAttribute(KV.Key, KV.Value);
                    }
                }*/

                NewTags.Add(Children[i].ConstructFromTemplate(this, TemplateInvoke));
            }

            return NewTags.ToArray();
        }
    }

    public class FMLTemplateValueTag : FMLTag {
        public FMLTemplateTag Template;

        public FMLTemplateValueTag(FMLTemplateTag RootTemplate) : base() {
            Template = RootTemplate;
        }

        public override FMLTag ConstructFromTemplate(FMLTemplateTag Template, FMLTag TemplateInvoke) {
            object Val = TemplateInvoke.Attributes.GetAttribute(TagName);

            if (Val == null)
                Val = Template.Attributes.GetAttribute(TagName);

            return new FMLValueTag(Val);
        }

        public override string ToString() {
            return "$" + base.ToString();
        }

        public override XmlElement ToXmlElement(XmlDocument Doc) {
            throw new InvalidOperationException();
        }
    }

    public class FMLValueTag : FMLTag {
        public object Value;

        public FMLValueTag(object Value) : base() {
            this.Value = Value;
        }

        public static string ConvertToString(object Value) {
            if (Value == null)
                return "none";
            else if (Value is string Str)
                return string.Format("\"{0}\"", Str.Replace("\"", "\\\""));
            else if (Value is int I)
                return I.ToString();
            else if (Value is float F)
                return F.ToString() + "f";
            else if (Value is FMLTemplateValue TV) {
                return "$" + TV.Name;
            } else if (Value is FMLHereDoc HD) {
                return HD.ToHereDocString();
            } else
                throw new Exception("Cannot convert value to string");
        }

        public override void BuildString(int IndentLvl, StringBuilder SB) {
            if (IndentLvl > 0)
                SB.Append(new string('\t', IndentLvl));

            SB.AppendLine(ConvertToString(Value) + ";");
        }

        public override FMLTag ConstructFromTemplate(FMLTemplateTag Template, FMLTag TemplateInvoke) {
            return new FMLValueTag(Value);
        }

        public override XmlElement ToXmlElement(XmlDocument Doc) {
            throw new InvalidOperationException();
        }

        public override string ToString() {
            return string.Format(ConvertToString(Value) + ";");
        }
    }

    public class FMLTemplateValue {
        public string Name;

        public FMLTemplateValue(string Name) {
            this.Name = Name;
        }
    }
}
