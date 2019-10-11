﻿using System.Text;
using System.Xml;

namespace kbinxmlcs
{
    public class XmlWriter
    {
        private XmlDocument _xmlDocument;
        private Encoding _encoding;

        private NodeBuffer _nodeBuffer;
        private DataBuffer _dataBuffer;

        public XmlWriter(XmlDocument xmlDocument, Encoding encoding)
        {
            _xmlDocument = xmlDocument;
            _encoding = encoding;

            _nodeBuffer = new NodeBuffer(true, encoding);
            _dataBuffer = new DataBuffer(encoding);
        }

        public byte[] Write()
        {
            Recurse(_xmlDocument.DocumentElement);
            _nodeBuffer.WriteU8(255);
            _nodeBuffer.Pad();
            _dataBuffer.Pad();

            //Write header data
            var output = new BigEndianBinaryBuffer();
            output.WriteU8(0xA0); //Magic
            output.WriteU8(0x42); //Compression flag
            output.WriteU8(EncodingDictionary.ReverseEncodingMap[_encoding]);
            output.WriteU8((byte)~EncodingDictionary.ReverseEncodingMap[_encoding]);

            //Write node buffer length and contents.
            output.WriteS32(_nodeBuffer.ToArray().Length);
            output.WriteBytes(_nodeBuffer.ToArray());

            //Write data buffer length and contents.
            output.WriteS32(_dataBuffer.ToArray().Length);
            output.WriteBytes(_dataBuffer.ToArray());

            return output.ToArray();
        }

        private void Recurse(XmlElement xmlElement)
        {
            var typestr = xmlElement.Attributes["__type"]?.Value;
            var sizestr = xmlElement.Attributes["__count"]?.Value;

            if (typestr == null)
            {
                _nodeBuffer.WriteU8(1);
                _nodeBuffer.WriteString(xmlElement.Name);
            }
            else
            {
                var typeid = TypeDictionary.ReverseTypeMap[typestr];
                if (sizestr != null)
                    _nodeBuffer.WriteU8((byte)(typeid | 0x40));
                else
                    _nodeBuffer.WriteU8(typeid);

                _nodeBuffer.WriteString(xmlElement.Name);
                if (typestr == "str")
                    _dataBuffer.WriteString(xmlElement.InnerText);
                else if (typestr == "bin")
                    _dataBuffer.WriteBinary(xmlElement.InnerText);
                else
                {
                    var type = TypeDictionary.TypeMap[typeid];
                    var value = xmlElement.InnerText.Split(' ');
                    var size = (uint)(type.Size * type.Count);

                    if (sizestr != null)
                    {
                        size *= uint.Parse(sizestr);
                        _dataBuffer.WriteU32(size);
                    }

                    for (var i = 0; i < size / type.Size; i++)
                        _dataBuffer.WriteBytes(type.GetBytes(value[i]));
                }
            }

            foreach (XmlAttribute attribute in xmlElement.Attributes)
            {
                if (attribute.Name != "str" || attribute.Name != "bin")
                {
                    _nodeBuffer.WriteU8(0x2E);
                    _nodeBuffer.WriteString(attribute.Name);
                    _dataBuffer.WriteString(attribute.Value);
                }
            }

            foreach (XmlNode childNode in xmlElement.ChildNodes)
            {
                if (childNode is XmlElement)
                    Recurse((XmlElement)childNode);
            }
            _nodeBuffer.WriteU8(0xBE);
        }
    }
}
