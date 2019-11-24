/**
 * Authors: David Bruck (dbruck1@@fau.edu) and Freguens Mildort (fmildort2015@@fau.edu)
 * Original source: https://github.com/CDA6122/Project
 * License: BSD 2-Clause License (https://opensource.org/licenses/BSD-2-Clause)
 **/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Project.Models
{
    internal interface IReadOnlyHashSet<T> : ISet<T>, IReadOnlyCollection<T>
    {
        IEqualityComparer<T> Comparer { get; }
        void CopyTo(T[] array);
        void CopyTo(T[] array, int arrayIndex, int count);
        bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue);
    }

    internal sealed class ReadOnlyHashSet<T> : IReadOnlyHashSet<T>
    {
        const string ErrorCollectionIsReadOnly = "Collection is read-only.";

        private readonly HashSet<T> set;
        internal ReadOnlyHashSet(HashSet<T> set) => this.set = set;

        public IEqualityComparer<T> Comparer => set.Comparer;
        public int Count => set.Count;
        public bool IsReadOnly => true;
        bool ISet<T>.Add(T item) => throw new NotSupportedException(ErrorCollectionIsReadOnly);
        void ICollection<T>.Clear() => throw new NotSupportedException(ErrorCollectionIsReadOnly);
        public bool Contains(T item) => set.Contains(item);
        public void CopyTo(T[] array) => set.CopyTo(array);
        public void CopyTo(T[] array, int arrayIndex) => set.CopyTo(array, arrayIndex);
        public void CopyTo(T[] array, int arrayIndex, int count) => set.CopyTo(array, arrayIndex, count);
        void ISet<T>.ExceptWith(IEnumerable<T> other) => throw new NotSupportedException(ErrorCollectionIsReadOnly);
        public IEnumerator<T> GetEnumerator() => set.GetEnumerator();
        void ISet<T>.IntersectWith(IEnumerable<T> other) => throw new NotSupportedException(ErrorCollectionIsReadOnly);
        public bool IsProperSubsetOf(IEnumerable<T> other) => set.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => set.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other) => set.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => set.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<T> other) => set.Overlaps(other);
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException(ErrorCollectionIsReadOnly);
        public bool SetEquals(IEnumerable<T> other) => set.SetEquals(other);
        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) =>
            throw new NotSupportedException(ErrorCollectionIsReadOnly);
        public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue) =>
            set.TryGetValue(equalValue, out actualValue);
        public void UnionWith(IEnumerable<T> other) => set.UnionWith(other);
        void ICollection<T>.Add(T item) => throw new NotSupportedException(ErrorCollectionIsReadOnly);
        IEnumerator IEnumerable.GetEnumerator() => set.GetEnumerator();
    }
}
