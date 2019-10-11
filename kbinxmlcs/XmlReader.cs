﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace kbinxmlcs
{
    public class XmlReader
    {
        private NodeBuffer _nodeBuffer;
        private DataBuffer _dataBuffer;

        private XmlDocument _xmlDocument = new XmlDocument();
        private XmlElement _currentElement;

        public XmlReader(byte[] buffer)
        {
            //Read header section.
            var binaryBuffer = new BigEndianBinaryBuffer(buffer);
            var signature = binaryBuffer.ReadU8();
            var compressionFlag = binaryBuffer.ReadU8();
            var encodingFlag = binaryBuffer.ReadU8();
            var encodingFlagNot = binaryBuffer.ReadU8();

            //Verify magic.
            if (signature != 0xA0)
                throw new KbinException($"Signature was invalid. {signature} != 0xA0");

            //Encoding flag should be an inverse of the fourth byte.
            if ((byte)~encodingFlag != encodingFlagNot)
                throw new KbinException($"Third byte was not an inverse of the fourth. {~encodingFlag} != {encodingFlagNot}");

            var compressed = compressionFlag == 0x42;
            var encoding = EncodingDictionary.EncodingMap[encodingFlag];

            //Get buffer lengths and load.
            var nodeLength = binaryBuffer.ReadS32();
            _nodeBuffer = new NodeBuffer(buffer.Skip(8).Take(nodeLength).ToArray(), compressed, encoding);

            var dataLength = BitConverter.ToInt32(buffer.Skip(nodeLength + 8).Take(4).Reverse().ToArray());
            _dataBuffer = new DataBuffer(buffer.Skip(nodeLength + 12).Take(dataLength).ToArray(), encoding);

            _xmlDocument.InsertBefore(_xmlDocument.CreateXmlDeclaration("1.0", encoding.WebName, null), _xmlDocument.DocumentElement);
        }

        public XmlDocument Read()
        {
            while (true)
            {
                var nodeType = _nodeBuffer.ReadU8();

                //Array flag is on the second bit
                var array = (nodeType & 64) > 0;
                nodeType = (byte)(nodeType & ~64);

                if (Enum.IsDefined(typeof(ControlType), nodeType))
                {
                    switch ((ControlType)nodeType)
                    {
                        case ControlType.NodeStart:
                            var newElement = _xmlDocument.CreateElement
                                (_nodeBuffer.ReadString(), null);

                            if (_currentElement != null)
                                _currentElement.AppendChild(newElement);
                            else
                                _xmlDocument.AppendChild(newElement);

                            _currentElement = newElement;
                            break;

                        case ControlType.Attribute:
                            var value = _dataBuffer.ReadString(_dataBuffer.ReadS32());
                            _currentElement.SetAttribute(_nodeBuffer.ReadString(), value);
                            break;

                        case ControlType.NodeEnd:
                            if (_currentElement.ParentNode is XmlDocument)
                                return _xmlDocument;

                            _currentElement = (XmlElement)_currentElement.ParentNode;
                            break;

                        case ControlType.FileEnd:
                            return _xmlDocument;
                    }
                }
                else if (TypeDictionary.TypeMap.TryGetValue(nodeType, out var propertyType))
                {
                    var elementName = _nodeBuffer.ReadString();
                    _currentElement = (XmlElement)_currentElement.AppendChild(_xmlDocument.CreateElement(elementName));

                    var attribute = _xmlDocument.CreateAttribute("__type");
                    attribute.Value = propertyType.Alias;
                    _currentElement.Attributes.Append(attribute);

                    var arraySize = propertyType.Size * propertyType.Count;
                    if (array || propertyType.Alias == "str" || propertyType.Alias == "bin")
                        arraySize = _dataBuffer.ReadS32(); //Total size.

                    if (propertyType.Alias == "str")
                        _currentElement.InnerText = _dataBuffer.ReadString(arraySize);
                    else if (propertyType.Alias == "bin")
                    {
                        _currentElement.InnerText = _dataBuffer.ReadBinary(arraySize);
                        _currentElement.SetAttribute("__size", arraySize.ToString());
                    }
                    else
                    {
                        if (array)
                        {
                            var size = (arraySize / (propertyType.Size * propertyType.Count)).ToString();
                            _currentElement.SetAttribute("__count", size);
                        }
                        var buffer = _dataBuffer.ReadBytes(arraySize);
                        var result = new List<string>();

                        for (var i = 0; i < arraySize / propertyType.Size; i++)
                            result.Add(propertyType.ToString(buffer.Skip(i * propertyType.Size)
                                .Take(propertyType.Size).ToArray()));
                        _currentElement.InnerText = string.Join(' ', result);
                    }
                }
                else
                {
                    throw new KbinException($"Unknown property type: {nodeType}");
                }
            }
        }
    }
}
