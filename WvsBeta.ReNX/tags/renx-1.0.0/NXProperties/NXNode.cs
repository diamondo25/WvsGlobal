// reNX is copyright angelsl, 2011 to 2012 inclusive.
// 
// This file is part of reNX.
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
// Linking this library statically or dynamically with other modules
// is making a combined work based on this library. Thus, the terms and
// conditions of the GNU General Public License cover the whole combination.
// 
// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent modules,
// and to copy and distribute the resulting executable under terms of your
// choice, provided that you also meet, for each linked independent module,
// the terms and conditions of the license of that module. An independent
// module is a module which is not derived from or based on this library.
// If you modify this library, you may extend this exception to your version
// of the library, but you are not obligated to do so. If you do not wish to
// do so, delete this exception statement from your version.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using Assembine;

namespace reNX.NXProperties
{
    /// <summary>
    ///   A node containing no value.
    /// </summary>
    public class NXNode : IEnumerable<NXNode>
    {
        private readonly ushort _childCount;

        /// <summary>
        ///   The NX file containing this node.
        /// </summary>
        protected readonly NXFile _file;

        private readonly uint _firstChild;
        private readonly string _name;
        private readonly NXNode _parent;
        private Dictionary<string, NXNode> _children;

        internal NXNode(string name, NXNode parent, NXFile file, ushort childCount, uint firstChildId)
        {
            _name = name;
            _parent = parent;
            _file = file;
            _childCount = childCount;
            _firstChild = firstChildId;
        }

        /// <summary>
        ///   The name of this node.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        ///   The parent node of this node, that is, the node containing this node as a child.
        /// </summary>
        public NXNode Parent
        {
            get { return _parent; }
        }

        /// <summary>
        ///   The file containing this node.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this property is accessed after the containing file is disposed.</exception>
        public NXFile File
        {
            get
            {
                _file.CheckDisposed();
                return _file;
            }
        }

        /// <summary>
        ///   The number of children contained in this node.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if this property is accessed after the containing file is disposed.</exception>
        public int ChildCount
        {
            get
            {
                _file.CheckDisposed();
                return _childCount;
            }
        }

        /// <summary>
        ///   Gets the child contained in this node that has the specified name.
        /// </summary>
        /// <param name="name"> The name of the child to get. </param>
        /// <returns> The child with the specified name. </returns>
        /// <exception cref="ObjectDisposedException">Thrown if this property is accessed after the containing file is disposed.</exception>
        /// <exception cref="KeyNotFoundException">The node does not contain a child with name
        ///   <paramref name="name" />
        ///   .</exception>
        public NXNode this[string name]
        {
            get
            {
                _file.CheckDisposed();
                if (_childCount > 0 && _children == null) CheckChild();
                return _children == null ? null : _children[name];
            }
        }

        #region IEnumerable<NXNode> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns> A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection. </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<NXNode> GetEnumerator()
        {
            if (_childCount > 0 && _children == null) CheckChild();
            return _children == null ? (IEnumerator<NXNode>)new NullEnumerator<NXNode>() : _children.Values.GetEnumerator();
        }

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns> An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection. </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///   Returns true if this node contains a child with the specified name.
        /// </summary>
        /// <param name="name"> The name of the child to check. </param>
        /// <returns> true if this node contains a child with the specified name; false otherwise </returns>
        /// <exception cref="ObjectDisposedException">Thrown if this method is called after the containing file is disposed.</exception>
        public bool ContainsChild(string name)
        {
            _file.CheckDisposed();
            if (_childCount > 0 && _children == null) CheckChild();
            return _children != null && _children.ContainsKey(name);
        }

        /// <summary>
        ///   Gets the child contained in this node that has the specified name.
        /// </summary>
        /// <param name="name"> The name of the child to get. </param>
        /// <returns> The child with the specified name. </returns>
        /// <exception cref="ObjectDisposedException">Thrown if this method is called after the containing file is disposed.</exception>
        public NXNode GetChild(string name)
        {
            return this[name];
        }

        private void AddChild(NXNode child)
        {
            _children.Add(child.Name, child);
        }

        private unsafe void CheckChild()
        {
            if (_children != null || _childCount < 1) return;
            _children = new Dictionary<string, NXNode>(_childCount);
            NodeData* start = _file._nodeBlock + _firstChild;
            for (ushort i = 0; i < _childCount; ++i, ++start)
                AddChild(ParseNode(start, this, _file));
        }

        internal static unsafe NXNode ParseNode(NodeData* ptr, NXNode parent, NXFile file)
        {
            NodeData nd = *ptr;
            string name = file.GetString(nd.NodeNameID);
            NXNode ret;
            switch (nd.Type) {
                case 0:
                    ret = new NXNode(name, parent, file, nd.ChildCount, nd.FirstChildID);
                    break;
                case 1:
                    ret = new NXValuedNode<long>(name, parent, file, nd.Type1Data, nd.ChildCount, nd.FirstChildID);
                    break;
                case 2:
                    ret = new NXValuedNode<double>(name, parent, file, nd.Type2Data, nd.ChildCount, nd.FirstChildID);
                    break;
                case 3:
                    ret = new NXStringNode(name, parent, file, nd.TypeIDData, nd.ChildCount, nd.FirstChildID);
                    break;
                case 4:
                    ret = new NXValuedNode<Point>(name, parent, file, new Point(nd.Type4DataX, nd.Type4DataY), nd.ChildCount, nd.FirstChildID);
                    break;
                case 5:
                    ret = new NXCanvasNode(name, parent, file, nd.TypeIDData, nd.Type5Width, nd.Type5Height, nd.ChildCount, nd.FirstChildID);
                    break;
                case 6:
                    ret = new NXMP3Node(name, parent, file, nd.TypeIDData, nd.Type4DataY, nd.ChildCount, nd.FirstChildID);
                    break;
                default:
                    return Util.Die<NXNode>(string.Format("NX node has invalid type {0}; dying", nd.Type));
            }

            if ((file._flags & NXReadSelection.EagerParseFile) == NXReadSelection.EagerParseFile)
                ret.CheckChild();

            return ret;
        }

        #region Nested type: NodeData

        [StructLayout(LayoutKind.Explicit, Size = 20, Pack = 2)]
        internal struct NodeData
        {
            [FieldOffset(0)]
            public readonly uint NodeNameID;

            [FieldOffset(4)]
            public readonly uint FirstChildID;

            [FieldOffset(8)]
            public readonly ushort ChildCount;

            [FieldOffset(10)]
            public readonly ushort Type;

            [FieldOffset(12)]
            public readonly long Type1Data;

            [FieldOffset(12)]
            public readonly double Type2Data;

            [FieldOffset(12)]
            public readonly uint TypeIDData;

            [FieldOffset(12)]
            public readonly int Type4DataX;

            [FieldOffset(16)]
            public readonly int Type4DataY;

            [FieldOffset(16)]
            public readonly ushort Type5Width;

            [FieldOffset(18)]
            public readonly ushort Type5Height;
        }

        #endregion
    }

    /// <summary>
    ///   A node containing a value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T"> The type of the contained value. </typeparam>
    public class NXValuedNode<T> : NXNode
    {
        /// <summary>
        ///   The value contained by this valued node.
        /// </summary>
        protected T _value;

        /// <summary>
        ///   Constructs an NX node without providing a value. For use by <see cref="NXLazyValuedNode{T}" /> only.
        /// </summary>
        /// <param name="name"> The name of the NX node. </param>
        /// <param name="parent"> The parent node of the NX node. </param>
        /// <param name="file"> The containing file of the NX node. </param>
        /// <param name="childCount"> The number of children this NX node contains. </param>
        /// <param name="firstChildId"> The Node ID of the first child of this NX node. </param>
        protected NXValuedNode(string name, NXNode parent, NXFile file, ushort childCount, uint firstChildId) : base(name, parent, file, childCount, firstChildId)
        {}

        internal NXValuedNode(string name, NXNode parent, NXFile file, T value, ushort childCount, uint firstChildId) : base(name, parent, file, childCount, firstChildId)
        {
            _value = value;
        }

        /// <summary>
        ///   The value contained by this node.
        /// </summary>
        public virtual T Value
        {
            get
            {
                _file.CheckDisposed();
                return _value;
            }
        }
    }

    /// <summary>
    ///   A node containing a lazily-loaded value of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T"> The type of the contained lazily-loaded value. </typeparam>
    public abstract class NXLazyValuedNode<T> : NXValuedNode<T>
    {
        /// <summary>
        ///   Whether the value of this lazy-loaded node has been loaded or not.
        /// </summary>
        protected bool _loaded;

        /// <summary>
        ///   Constructs a lazily-loaded node.
        /// </summary>
        /// <param name="name"> The name of the lazily-loaded node. </param>
        /// <param name="parent"> The parent node of the lazily-loaded node. </param>
        /// <param name="file"> The containing file of the lazily-loaded node. </param>
        /// <param name="childCount"> The number of children this lazily-loaded node contains. </param>
        /// <param name="firstChildId"> The Node ID of the first child of this node. </param>
        protected NXLazyValuedNode(string name, NXNode parent, NXFile file, ushort childCount, uint firstChildId) : base(name, parent, file, childCount, firstChildId)
        {}

        /// <summary>
        ///   The value contained by this node. If the value has not been loaded, the value will be loaded.
        /// </summary>
        public override T Value
        {
            get
            {
                _file.CheckDisposed();
                CheckLoad();
                return _value;
            }
        }

        /// <summary>
        ///   Checks if this node's value has been loaded, and if not, loads the value.
        /// </summary>
        protected void CheckLoad()
        {
            if (!_loaded)
                lock (_file._lock) {
                    if (_loaded) return;
                    _loaded = true;
                    _value = LoadValue();
                }
        }

        /// <summary>
        ///   Loads this value's node into memory.
        /// </summary>
        /// <returns> </returns>
        protected abstract T LoadValue();
    }

    /// <summary>
    ///   This class contains methods to simplify casting and retrieving of values from NX nodes.
    /// </summary>
    public static class NXValueHelper
    {
        /// <summary>
        ///   Tries to cast this NXNode to a <see cref="NXValuedNode{T}" /> and returns its value, or returns the default value if the cast is invalid.
        /// </summary>
        /// <typeparam name="T"> The type of the value to return. </typeparam>
        /// <param name="n"> This NXNode. </param>
        /// <param name="def"> The default value to return should the cast fail. </param>
        /// <returns> The contained value if the cast succeeds, or <paramref name="def" /> if the cast fails. </returns>
        public static T ValueOrDefault<T>(this NXNode n, T def)
        {
            NXValuedNode<T> nxvn = n as NXValuedNode<T>;
            return nxvn != null ? nxvn.Value : def;
        }

        /// <summary>
        ///   Tries to cast this NXNode to a <see cref="NXValuedNode{T}" /> and returns its value, or throws an <see
        ///    cref="InvalidCastException" /> if the cast is invalid.
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