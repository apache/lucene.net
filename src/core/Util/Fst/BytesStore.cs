﻿using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Util.Fst
{
    public class BytesStore : DataOutput
    {
        private readonly List<byte[]> blocks = new List<byte[]>();

        private readonly int blockSize;
        private readonly int blockBits;
        private readonly int blockMask;

        private byte[] current;
        private int nextWrite;

        public BytesStore(int blockBits)
        {
            this.blockBits = blockBits;
            blockSize = 1 << blockBits;
            blockMask = blockSize - 1;
            nextWrite = blockSize;
        }

        public BytesStore(DataInput input, long numBytes, int maxBlockSize)
        {
            var blockSize = 2;
            var blockBits = 1;
            while (blockSize < numBytes && blockSize < maxBlockSize)
            {
                blockSize *= 2;
                blockBits++;
            }
            this.blockBits = blockBits;
            this.blockSize = blockSize;
            this.blockMask = blockSize - 1;
            var left = numBytes;
            while (left > 0)
            {
                var chunk = (int) Math.Min(blockSize, left);
                var block = new byte[chunk];
                input.ReadBytes(block, 0, block.Length);
                blocks.Add(block);
                left -= chunk;
            }

            nextWrite = blocks[blocks.Count - 1].Length;
        }

        public void WriteByte(int dest, byte b)
        {
            var blockIndex = dest >> blockBits;
            var block = blocks[blockIndex];
            block[dest & blockMask] = b;
        }


        public override void WriteByte(byte b)
        {
            if (nextWrite == blockSize)
            {
                current = new byte[blockSize];
                blocks.Add(current);
                nextWrite = 0;
            }
            current[nextWrite++] = b;
        }


        public override void WriteBytes(byte[] b, int offset, int len)
        {
            while (len > 0)
            {
                var chunk = blockSize - nextWrite;
                if (len <= chunk)
                {
                    System.Buffer.BlockCopy(b, offset, current, nextWrite, len);
                    nextWrite += len;
                    break;
                }
                else
                {
                    if (chunk > 0)
                    {
                        System.Buffer.BlockCopy(b, offset, current, nextWrite, chunk);
                        offset += chunk;
                        len -= chunk;
                    }
                    current = new byte[blockSize];
                    blocks.Add(current);
                    nextWrite = 0;
                }
            }
        }

        internal int GetBlockBits()
        {
            return blockBits;
        }


       

        internal void WriteBytes(long dest, byte[] b, int offset, int len)
        {
            Debug.Assert(dest + len <= GetPosition(), "dest=" + dest + " pos=" + GetPosition() + " len=" + len);

            var end = dest + len;
            var blockIndex = (int) (end >> blockBits);
            var downTo = (int) (end & blockMask);
            if (downTo == 0)
            {
                blockIndex--;

                downTo = blockSize;
            }
            var block = blocks[blockIndex];

            while (len > 0)
            {
                if (len <= downTo)
                {
                    System.Buffer.BlockCopy(b, offset, block, downTo - len, len);
                    break;
                }
                else
                {
                    len -= downTo;
                    System.Buffer.BlockCopy(b, offset + len, block, 0, downTo);
                    blockIndex--;
                    block = blocks[blockIndex];
                    downTo = blockSize;
                }
            }
        }

        public void CopyBytes(long src, long dest, int len)
        {
            Debug.Assert(src < dest);

            var end = src + len;

            var blockIndex = (int) (end >> blockBits);
            var downTo = (int) (end & blockMask);
            if (downTo == 0)
            {
                blockIndex--;
                downTo = blockSize;
            }
            var block = blocks[blockIndex];

            while (len > 0)
            {
                if (len <= downTo)
                {
                    WriteBytes(dest, block, downTo - len, len);
                    break;
                }
                else
                {
                    len -= downTo;
                    WriteBytes(dest + len, block, 0, downTo);
                    blockIndex--;
                    block = blocks[blockIndex];
                    downTo = blockSize;
                }
            }
        }

        public void WriteInt(long pos, int value)
        {
            var blockIndex = (int) (pos >> blockBits);
            var upto = (int) (pos & blockMask);
            var block = blocks[blockIndex];
            var shift = 24;
            for (var i = 0; i < 4; i++)
            {
                block[upto++] =  (byte)(value >> shift);
                shift -= 8;
                if (upto == blockSize)
                {
                    upto = 0;
                    blockIndex++;
                    block = blocks[blockIndex];
                }
            }
        }

        public void Reverse(long srcPos, long destPos)
        {
            // TODO: assert correct here?
            Debug.Assert(srcPos < destPos);
            Debug.Assert(destPos < GetPosition());

            var srcBlockIndex = (int) (srcPos >> blockBits);
            var src = (int) (srcPos & blockMask);
            var srcBlock = blocks[srcBlockIndex];

            var destBlockIndex = (int) (destPos >> blockBits);
            var dest = (int) (destPos & blockMask);
            var destBlock = blocks[destBlockIndex];

            var limit = (int) (destPos - srcPos + 1)/2;
            for (var i = 0; i < limit; i++)
            {
                var b = srcBlock[src];
                srcBlock[src] = destBlock[dest];
                destBlock[dest] = b;
                src++;
                if (src == blockSize)
                {
                    srcBlockIndex++;
                    srcBlock = blocks[srcBlockIndex];
                    src = 0;
                }

                dest--;
                if (dest == -1)
                {
                    destBlockIndex--;
                    destBlock = blocks[destBlockIndex];
                    dest = blockSize - 1;
                }
            }
        }

        public void SkipBytes(int len)
        {
            while (len > 0)
            {
                var chunk = blockSize - nextWrite;
                if (len <= chunk)
                {
                    nextWrite += len;
                    break;
                }
                else
                {
                    len -= chunk;
                    current = new byte[blockSize];
                    blocks.Add(current);
                    nextWrite = 0;
                }
            }
        }

        public long GetPosition()
        {
            return ((long) blocks.Count - 1)*blockSize + nextWrite;
        }


        public void Truncate(long newLen)
        {
            // TODO: assert correct here?
            Debug.Assert(newLen <= GetPosition());
            Debug.Assert(newLen >= GetPosition());

            var blockIndex = (int) (newLen >> blockBits);
            nextWrite = (int) (newLen & blockMask);
            if (nextWrite == 0)
            {
                blockIndex--;
                nextWrite = blockSize;
            }
            blocks.SubList(blockIndex + 1, blocks.Count).Clear();
            if (newLen == 0)
            {
                current = null;
            }
            else
            {
                current = blocks[blockIndex];
            }
            // TODO: assert correct here?
            Debug.Assert(newLen == GetPosition());
        }

        public void Finish()
        {
            if (current != null)
            {
                var lastBuffer = new byte[nextWrite];
                System.Buffer.BlockCopy(current, 0, lastBuffer, 0, nextWrite);
                blocks[blocks.Count - 1] = lastBuffer;
                current = null;
            }
        }

        public void WriteTo(DataOutput output)
        {
            foreach (var block in blocks)
            {
                output.WriteBytes(block, 0, block.Length);
            }
        }

        private class AnonForwardBytesReader : FST.BytesReader
        {
            private byte[] current;
            private int nextBuffer;
            private int nextRead;

            private readonly BytesStore _parent;

            public AnonForwardBytesReader(BytesStore parent)
            {
                _parent = parent;
                nextRead = _parent.blockSize;
            }

            public override byte ReadByte()
            {
                if (nextRead == _parent.blockSize)
                {
                    current = _parent.blocks[nextBuffer++];
                    nextRead = 0;
                }
                return (byte)current[nextRead++];
            }

            public override void SkipBytes(int count)
            {
                Position = Position + count;
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                while (len > 0)
                {
                    var chunkLeft = _parent.blockSize - nextRead;
                    if (len <= chunkLeft)
                    {
                        System.Buffer.BlockCopy(current, nextRead, b, offset, len);
                        nextRead += len;
                        break;
                    }
                    else
                    {
                        if (chunkLeft > 0)
                        {
                            System.Buffer.BlockCopy(current, nextRead, b, offset, chunkLeft);
                            offset += chunkLeft;
                            len -= chunkLeft;
                        }
                        current = _parent.blocks[nextBuffer++];
                        nextRead = 0;
                    }
                }
            }

            public override long Position
            {
                get { return ((long) nextBuffer - 1)*_parent.blockSize + nextRead; }
                set
                {
                    var bufferIndex = (int) (value >> _parent.blockBits);
                    nextBuffer = bufferIndex + 1;
                    current = _parent.blocks[bufferIndex];
                    nextRead = (int) (value & _parent.blockMask);
                    // TODO: assert correct here?
                    Debug.Assert(Position == value);
                }
            }

            public override bool Reversed()
            {
                return false;
            }
        }

        public FST.BytesReader GetForwardReader()
        {
            if (blocks.Count == 1)
            {   
                // TODO: CLSCompliance
                return new ForwardBytesReader(blocks[0].Select(o => (sbyte)o).ToArray());
            }
            return new AnonForwardBytesReader(this);
        }


        private class AnonReverseBytesReader : FST.BytesReader
        {
            private byte[] current;
            private int nextBuffer = -1;
            private int nextRead = 0;


            private readonly BytesStore _parent;
            public AnonReverseBytesReader(BytesStore parent)
            {
                _parent = parent;
                current = _parent.blocks.Count == 0 ? null : _parent.blocks[0];
            }

            public override long Position
            {
                get
                {
                    return ((long)nextBuffer + 1) * _parent.blockSize + nextRead;
                }
                set
                {
                    var bufferIndex = (int)(value >> _parent.blockBits);
                    nextBuffer = bufferIndex - 1;
                    current = _parent.blocks[bufferIndex];
                    nextRead = (int)(value & _parent.blockMask);
                    // TODO: assert correct here?
                    Debug.Assert(Position == value, "value=" + value + " Position=" + Position);
                }
            }

            public override byte ReadByte()
            {
                if (nextRead == -1)
                {
                    current = _parent.blocks[nextBuffer--];
                    nextRead = _parent.blockSize - 1;
                }
                return (byte)current[nextRead--];
            }

            public override void SkipBytes(int count)
            {
                Position = Position - count;
            }

            public override void ReadBytes(byte[] b, int offset, int len)
            {
                for (var i = 0; i < len; i++)
                {
                    b[offset + i] = ReadByte();
                }
            }

            public override bool Reversed()
            {
                return true;
            }
        }

        public FST.BytesReader GetReverseReader()
        {
            return GetReverseReader(true);
        }

        public FST.BytesReader GetReverseReader(bool allowSingle)
        {
            if (allowSingle && blocks.Count == 1)
            {
                var result = blocks[0];
                return new ReverseBytesReader(result);
            }
            return new AnonReverseBytesReader(this);
        }
    }
}