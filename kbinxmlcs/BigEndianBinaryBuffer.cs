﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace kbinxmlcs
{
    internal class BigEndianBinaryBuffer
    {
        protected List<byte> _buffer;
        protected int _offset;

        internal BigEndianBinaryBuffer(byte[] buffer) => _buffer = new List<byte>(buffer);

        internal BigEndianBinaryBuffer() => _buffer = new List<byte>();

        internal virtual byte[] ReadBytes(int count)
        {
            byte[] buffer = _buffer.Skip(_offset).Take(count).ToArray();
            _offset += count;

            return buffer;
        }

        internal virtual void WriteBytes(byte[] buffer)
        {
            _buffer.InsertRange(_offset, buffer);
            _offset += buffer.Length;
        }

        internal virtual void WriteS8(sbyte value) => WriteBytes(new byte[] { (byte)value });

        internal virtual void WriteS16(short value) => WriteBytes(BitConverter.GetBytes(value).Reverse().ToArray());

        internal virtual void WriteS32(int value) => WriteBytes(BitConverter.GetBytes(value).Reverse().ToArray());

        internal virtual void WriteS64(long value) => WriteBytes(BitConverter.GetBytes(value).Reverse().ToArray());

        internal virtual void WriteU8(byte value) => WriteBytes(new byte[] { value });

        internal virtual void WriteU16(ushort value) => WriteBytes(BitConverter.GetBytes(value).Reverse().ToArray());

        internal virtual void WriteU32(uint value) => WriteBytes(BitConverter.GetBytes(value).Reverse().ToArray());

        internal virtual void WriteU64(ulong value) => WriteBytes(BitConverter.GetBytes(value).Reverse().ToArray());

        internal virtual sbyte ReadS8() => (sbyte)ReadBytes(sizeof(byte))[0];

        internal virtual short ReadS16() => BitConverter.ToInt16(ReadBytes(sizeof(short)).Reverse().ToArray());

        internal virtual int ReadS32() => BitConverter.ToInt32(ReadBytes(sizeof(int)).Reverse().ToArray());

        internal virtual long ReadS64() => BitConverter.ToInt64(ReadBytes(sizeof(long)).Reverse().ToArray());

        internal virtual byte ReadU8() => ReadBytes(sizeof(byte))[0];

        internal virtual ushort ReadU16() => BitConverter.ToUInt16(ReadBytes(sizeof(short)).Reverse().ToArray());

        internal virtual uint ReadU32() => BitConverter.ToUInt32(ReadBytes(sizeof(int)).Reverse().ToArray());

        internal virtual ulong ReadU64() => BitConverter.ToUInt64(ReadBytes(sizeof(long)).Reverse().ToArray());

        internal void Pad()
        {
            while (_buffer.Count % 4 != 0)
                _buffer.Add(0);
        }

        internal byte[] ToArray() => _buffer.ToArray();

        internal int Length => _buffer.Count();

        internal byte this[int index] => _buffer[index];
    }
}