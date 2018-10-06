// reNX is copyright angelsl, 2011 to 2013 inclusive.
// 
// This file (NXNode.cs) is part of reNX.
// 
// reNX is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// reNX is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with reNX. If not, see <http://www.gnu.org/licenses/>.
// 
// Linking reNX statically or dynamically with other modules
// is making a combined work based on reNX. Thus, the terms and
// conditions of the GNU General Public License cover the whole combination.
// 
// As a special exception, the copyright holders of reNX give you
// permission to link reNX with independent modules to produce an
// executable, regardless of the license terms of these independent modules,
// and to copy and distribute the resulting executable under terms of your
// choice, provided that you also meet, for each linked independent module,
// the terms and conditions of the license of that module. An independent
// module is a module which is not derived from or based on reNX.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace reNX.NXProperties
{
    /// <summary>
    ///     A node containing no value.
    /// </summary>
    public class NXNode : IEnumerable<NXNode>
    {

        private class ChildEnumerator : IEnumerator<NXNode>
        {

            private int _id = -1;
            private NXNode _node;

            public void Dispose()
            {
            }

            public unsafe bool MoveNext()
            {
                ++_id;
                return _id > -1 && _id < _node._nodeData->ChildCount;
            }

            public void Reset()
            {
                _id = -1;
            }

            public unsafe NXNode Current => _node._file._nodes[_node._nodeData->FirstChildID + _id];

            object IEnumerator.Current => Current;

            public ChildEnumerator(NXNode n)
            {
                _node = n;
            }

        }

        /// <summary>
        ///     The NX file containing this node.
        /// </summary>
        protected readonly NXFile _file;

        /// <summary>
        ///     The pointer to the <see cref="NodeData" /> describing this node.
        /// </summary>
        protected readonly unsafe NodeData* _nodeData;

        private Dictionary<string, NXNode> _children;
        private bool _childinit;
        private object _lockObject = 1;
        public bool IsExternallyLoaded = false;

        internal unsafe NXNode(NodeData* ptr, NXFile file)
        {
            _nodeData = ptr;
            _file = file;
            // Don't care to load if there are no kids
            _childinit = _nodeData->ChildCount == 0;
        }

        private static readonly CultureInfo ConversionCultureInfo = CultureInfo.InvariantCulture;

        private long ValueAsLong()
        {
            if (this is /*sparta*/ NXValuedNode<Int64>) return ((NXValuedNode<Int64>)this).Value;
            if (this is NXValuedNode<String>) return long.Parse(((NXValuedNode<String>)this).Value, ConversionCultureInfo.NumberFormat);
            if (this is NXValuedNode<Double>) return (long)((NXValuedNode<Double>)this).Value;
            throw new NotImplementedException("Unable to use this value as a long. " + this.GetType());
        }

        private double ValueAsDouble()
        {
            if (this is NXValuedNode<Double>) return ((NXValuedNode<Double>)this).Value;
            if (this is NXValuedNode<Int64>) return ((NXValuedNode<Int64>)this).Value;
            if (this is NXValuedNode<String>)
            {
                var str = ((NXValuedNode<String>)this).Value;
                if (str.StartsWith("[R8]")) return double.Parse(str.Substring(4), ConversionCultureInfo.NumberFormat);

                return double.Parse(str, ConversionCultureInfo.NumberFormat);
            }
            throw new NotImplementedException("Unable to use this value as a double. " + this.GetType());
        }

        private string ValueAsString()
        {
            if (this is NXValuedNode<String>) return ((NXValuedNode<String>)this).Value;
            if (this is NXValuedNode<Double>) return ((NXValuedNode<Double>)this).Value.ToString(ConversionCultureInfo);
            if (this is NXValuedNode<Int64>) return ((NXValuedNode<Int64>)this).Value.ToString(ConversionCultureInfo);
            if (this is NXValuedNode<Point>) return ((NXValuedNode<Point>)this).Value.ToString();
            throw new NotImplementedException("Unable to use this value as a string. " + this.GetType());
        }

        public string ValueString() => ValueAsString();
        public double ValueDouble() => ValueAsDouble();

        public float ValueFloat() => (float)ValueDouble();

        public long ValueInt64() => ValueAsLong();
        public int ValueInt32() => (int)ValueInt64();
        public short ValueInt16() => (short)ValueInt64();
        public uint ValueUInt32() => (uint)ValueInt64();
        public ushort ValueUInt16() => (ushort)ValueInt64();
        public byte ValueUInt8() => ValueByte();
        public sbyte ValueInt8() => ValueSByte();
        public byte ValueByte() => (byte)ValueInt64();
        public sbyte ValueSByte() => (sbyte)ValueInt64();
        public bool ValueBool() => ValueInt64() > 0;

        /// <summary>
        ///     The name of this node.
        /// </summary>
        public unsafe string Name => _file.GetString(_nodeData->NodeNameID);

        /// <summary>
        ///     The file containing this node.
        /// </summary>
        public NXFile File => _file;

        /// <summary>
        ///     The number of children contained in this node.
        /// </summary>
        /// <exception cref="AccessViolationException">Thrown if this property is accessed after the containing file is disposed.</exception>
        public unsafe int ChildCount => _nodeData->ChildCount;

        public unsafe bool IsLoaded => _nodeData->ChildCount == 0 || _children != null;

        /// <summary>
        ///     Gets the child contained in this node that has the specified name.
        /// </summary>
        /// <param name="name"> The name of the child to get. </param>
        /// <returns> The child with the specified name. Or Null.</returns>
        /// <exception cref="AccessViolationException">Thrown if this property is accessed after the containing file is disposed.</exception>
        public unsafe NXNode this[string name]
        {
            get
            {
                lock (_lockObject)
                {
                    if (!IsLoaded) CheckChild(!_childinit, true);
                    if (_children.TryGetValue(name, out var node)) return node;
                    return null;
                }
            }
            set
            {
                lock (_lockObject)
                {
                    if (!IsLoaded) CheckChild(!_childinit, true);
                    // else if (_children == null) _children = new Dictionary<string, NXNode>();
                    _children[name] = value;
                }
            }
        }


        public unsafe bool IsSameAs(NXNode otherNode)
        {
            if (_nodeData->Type != otherNode._nodeData->Type) return false;

            switch (_nodeData->Type)
            {
                // Just a node
                case 0: return true;

                case 1: return _nodeData->Type1Data == otherNode._nodeData->Type1Data;
                case 2: return _nodeData->Type2Data == otherNode._nodeData->Type2Data;

                // Check strings manually
                case 3: return ValueAsString() == otherNode.ValueAsString();
                case 4:
                    return (_nodeData->Type4DataX == otherNode._nodeData->Type4DataX) &&
                           (_nodeData->Type4DataY == otherNode._nodeData->Type4DataY);
                case 5:
                    return (_nodeData->Type5Width == otherNode._nodeData->Type5Width) &&
                           (_nodeData->Type5Height == otherNode._nodeData->Type5Height);

                case 6: // Nobody cares about audio
                default: return true;
            }


            return true;
        }

        public void Unload(bool alsoChildNodes)
        {
            if (_children != null)
            {
                if (alsoChildNodes)
                {
                    foreach (var subNode in _children)
                    {
                        subNode.Value.Unload(true);
                    }
                }
                _childinit = false;
                _children = null;
            }

        }

        #region IEnumerable<NXNode> Members

        private static IEnumerator<NXNode> emptyEnumerator = Enumerable.Empty<NXNode>().GetEnumerator();

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        /// <exception cref="AccessViolationException">Thrown if this property is accessed after the containing file is disposed.</exception>
        /// <filterpriority>1</filterpriority>
        public unsafe IEnumerator<NXNode> GetEnumerator()
        {
            if (_nodeData->ChildCount == 0) return emptyEnumerator;

            lock (_lockObject)
            {
                if (!IsLoaded) CheckChild(true, true);
            }

            if (!IsExternallyLoaded)
            {
                return new ChildEnumerator(this);
            }

            return _children.Values.GetEnumerator();
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        /// <exception cref="AccessViolationException">Thrown if this property is accessed after the containing file is disposed.</exception>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///     Returns true if this node contains a child with the specified name.
        /// </summary>
        /// <param name="name"> The name of the child to check. </param>
        /// <returns> true if this node contains a child with the specified name; false otherwise </returns>
        /// <exception cref="AccessViolationException">Thrown if this property is accessed after the containing file is disposed.</exception>
        public unsafe bool ContainsChild(string name)
        {
            lock (_lockObject)
            {
                if (_nodeData->ChildCount == 0) return false;
                if (_children == null) CheckChild(!_childinit, true);
                return _children.ContainsKey(name);
            }
        }

        /// <summary>
        ///     Gets the child contained in this node that has the specified name.
        /// </summary>
        /// <param name="name"> The name of the child to get. </param>
        /// <returns> The child with the specified name. </returns>
        /// <exception cref="AccessViolationException">Thrown if this property is accessed after the containing file is disposed.</exception>
        public NXNode GetChild(string name)
        {
            return this[name];
        }

        private void AddChild(NXNode child)
        {
            _children[child.Name] = child;
        }

        private unsafe void CheckChild(bool parse = true, bool map = false)
        {
            // ugly code begins here
            NodeData* start = _file._nodeBlock + _nodeData->FirstChildID;
            long end = _nodeData->ChildCount + _nodeData->FirstChildID;
            switch ((parse ? 1 : 0) | (map ? 2 : 0))
            {
                case 1:
                    for (uint i = _nodeData->FirstChildID; i < end; ++i, ++start)
                        if (_file._nodes[i] == null) _file._nodes[i] = ParseNode(start, this, _file);
                    _childinit = true;
                    break;
                case 2:
                    _children = new Dictionary<string, NXNode>(_nodeData->ChildCount);
                    for (uint i = _nodeData->FirstChildID; i < end; ++i)
                    {
                        AddChild(_file._nodes[i]);
                    }
                    break;
                case 3:
                    _children = new Dictionary<string, NXNode>(_nodeData->ChildCount);
                    for (uint i = _nodeData->FirstChildID; i < end; ++i, ++start)
                    {
                        if (_file._nodes[i] == null)
                        {
                            _file._nodes[i] = ParseNode(start, this, _file);
                        }
                        AddChild(_file._nodes[i]);
                    }
                    _childinit = true;
                    break;
                default:
                    Util.Die("This should never happen; CheckChild");
                    break;
            }
            // ugly code ends here
        }

        internal static unsafe NXNode ParseNode(NodeData* ptr, NXNode parent, NXFile file)
        {
            NXNode ret;
            switch (ptr->Type)
            {
                case 0:
                    ret = new NXNode(ptr, file);
                    break;
                case 1:
                    ret = new NXInt64Node(ptr, file);
                    break;
                case 2:
                    ret = new NXDoubleNode(ptr, file);
                    break;
                case 3:
                    ret = new NXStringNode(ptr, file);
                    break;
                case 4:
                    ret = new NXPointNode(ptr, file);
                    break;
                case 5:
                    ret = new NXBitmapNode(ptr, file);
                    break;
                case 6:
                    ret = new NXAudioNode(ptr, file);
                    break;
                default:
                    return Util.Die<NXNode>(string.Format("NX node has invalid type {0}; dying", ptr->Type));
            }

            if ((file._flags & NXReadSelection.EagerParseFile) == NXReadSelection.EagerParseFile) ret.CheckChild();

            return ret;
        }

        #region Nested type: NodeData

        /// <summary>
        ///     This structure describes a node.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 20, Pack = 2)]
        protected internal struct NodeData
        {
            [FieldOffset(0)]
            internal readonly uint NodeNameID;

            [FieldOffset(4)]
            internal readonly uint FirstChildID;

            [FieldOffset(8)]
            internal readonly ushort ChildCount;

            [FieldOffset(10)]
            internal readonly ushort Type;

            [FieldOffset(12)]
            internal readonly long Type1Data;

            [FieldOffset(12)]
            internal readonly double Type2Data;

            [FieldOffset(12)]
            internal readonly uint TypeIDData;

            [FieldOffset(12)]
            internal readonly int Type4DataX;

            [FieldOffset(16)]
            internal readonly int Type4DataY;

            [FieldOffset(16)]
            internal readonly ushort Type5Width;

            [FieldOffset(18)]
            internal readonly ushort Type5Height;
        }

        #endregion
    }

    /// <summary>
    ///     A node containing a value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T"> The type of the contained value. </typeparam>
    public abstract class NXValuedNode<T> : NXNode
    {
        internal unsafe NXValuedNode(NodeData* ptr, NXFile file) : base(ptr, file) { }

        /// <summary>
        ///     The value contained by this node.
        /// </summary>
        public abstract T Value { get; }
    }

    /// <summary>
    ///     A node containing a lazily-loaded value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T"> The type of the contained lazily-loaded value. </typeparam>
    public abstract class NXLazyValuedNode<T> : NXValuedNode<T> where T : class
    {
        /// <summary>
        ///     The value contained in this lazily-loaded node.
        /// </summary>
        protected T _value;

        internal unsafe NXLazyValuedNode(NodeData* ptr, NXFile file) : base(ptr, file) { }

        /// <summary>
        ///     The value contained by this node. If the value has not been loaded, the value will be loaded.
        /// </summary>
        public override T Value
        {
            get { lock (_file._lock) return _value ?? (_value = LoadValue()); }
        }

        /// <summary>
        ///     Loads this value's node into memory.
        /// </summary>
        /// <returns> </returns>
        protected abstract T LoadValue();
    }

    /// <summary>
    ///     This class contains methods to simplify casting and retrieving of values from NX nodes.
    /// </summary>
    public static class NXValueHelper
    {
        /// <summary>
        ///     Tries to cast this NXNode to a <see cref="NXValuedNode{T}" /> and returns its value, or returns the default value if the cast is invalid.
        /// </summary>
        /// <typeparam name="T"> The type of the value to return. </typeparam>
        /// <param name="n"> This NXNode. </param>
        /// <param name="def"> The default value to return should the cast fail. </param>
        /// <returns>
        ///     The contained value if the cast succeeds, or <paramref name="def" /> if the cast fails.
        /// </returns>
        public static T ValueOrDefault<T>(this NXNode n, T def)
        {
            var nxvn = n as NXValuedNode<T>;
            return nxvn != null ? nxvn.Value : def;
        }

        /// <summary>
        ///     Tries to cast this NXNode to a <see cref="NXValuedNode{T}" /> and returns its value, or throws an
        ///     <see cref="InvalidCastException" />
        ///     if the cast is invalid.
        /// </summary>
        /// <typeparam name="T"> The type of the value to return. </typeparam>
        /// <param name="n"> This NXNode. </param>
        /// <returns> The contained value if the cast succeeds. </returns>
        /// <exception cref="InvalidCastException">Thrown if the cast is invalid.</exception>
        public static T ValueOrDie<T>(this NXNode n)
        {
            return ((NXValuedNode<T>)n).Value;
        }
    }
}
