using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace OpenUtau.Core.Lib {
	[DebuggerDisplay("Count = {Count}, Capacity = {Capacity}")]
	[DebuggerTypeProxy(typeof(Deque<>.DebugView))]
	internal sealed class Deque<T> : IList<T>, System.Collections.IList {
		private const int DefaultCapacity = 8;

		private T[] buffer;

		private int offset;

		public Deque(int capacity) {
			if (capacity < 1)
				throw new ArgumentOutOfRangeException("capacity", "Capacity must be greater than 0.");
			buffer = new T[capacity];
		}

		public Deque(IEnumerable<T> collection) {
			int count = collection.Count();
			if (count > 0) {
				buffer = new T[count];
				DoInsertRange(0, collection, count);
			} else {
				buffer = new T[DefaultCapacity];
			}
		}

		public Deque()
			: this(DefaultCapacity) {
		}


		bool ICollection<T>.IsReadOnly {
			get { return false; }
		}

		public T this[int index] {
			get {
				CheckExistingIndexArgument(this.Count, index);
				return DoGetItem(index);
			}

			set {
				CheckExistingIndexArgument(this.Count, index);
				DoSetItem(index, value);
			}
		}

		public void Insert(int index, T item) {
			CheckNewIndexArgument(Count, index);
			DoInsert(index, item);
		}

		public void RemoveAt(int index) {
			CheckExistingIndexArgument(Count, index);
			DoRemoveAt(index);
		}

		public int IndexOf(T item) {
			var comparer = EqualityComparer<T>.Default;
			int ret = 0;
			foreach (var sourceItem in this) {
				if (comparer.Equals(item, sourceItem))
					return ret;
				++ret;
			}

			return -1;
		}

		void ICollection<T>.Add(T item) {
			DoInsert(Count, item);
		}

		bool ICollection<T>.Contains(T item) {
			return this.Contains(item, null);
		}

		void ICollection<T>.CopyTo(T[] array, int arrayIndex) {
			if (array == null)
				throw new ArgumentNullException("array", "Array is null");

			int count = this.Count;
			CheckRangeArguments(array.Length, arrayIndex, count);
			for (int i = 0; i != count; ++i) {
				array[arrayIndex + i] = this[i];
			}
		}

		public bool Remove(T item) {
			int index = IndexOf(item);
			if (index == -1)
				return false;

			DoRemoveAt(index);
			return true;
		}

		public IEnumerator<T> GetEnumerator() {
			int count = this.Count;
			for (int i = 0; i != count; ++i) {
				yield return DoGetItem(i);
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}


		private bool ObjectIsT(object item) {
			if (item is T) {
				return true;
			}

			if (item == null) {
				var type = typeof(T);
				if (type.IsClass && !type.IsPointer)
					return true; // classes, arrays, and delegates
				if (type.IsInterface)
					return true; // interfaces
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
					return true; // nullable value types
			}

			return false;
		}

		int System.Collections.IList.Add(object value) {
			if (!ObjectIsT(value))
				throw new ArgumentException("Item is not of the correct type.", "value");
			AddToBack((T)value);
			return Count - 1;
		}

		bool System.Collections.IList.Contains(object value) {
			if (!ObjectIsT(value))
				throw new ArgumentException("Item is not of the correct type.", "value");
			return this.Contains((T)value);
		}

		int System.Collections.IList.IndexOf(object value) {
			if (!ObjectIsT(value))
				throw new ArgumentException("Item is not of the correct type.", "value");
			return IndexOf((T)value);
		}

		void System.Collections.IList.Insert(int index, object value) {
			if (!ObjectIsT(value))
				throw new ArgumentException("Item is not of the correct type.", "value");
			Insert(index, (T)value);
		}

		bool System.Collections.IList.IsFixedSize {
			get { return false; }
		}

		bool System.Collections.IList.IsReadOnly {
			get { return false; }
		}

		void System.Collections.IList.Remove(object value) {
			if (!ObjectIsT(value))
				throw new ArgumentException("Item is not of the correct type.", "value");
			Remove((T)value);
		}

		object System.Collections.IList.this[int index] {
			get {
				return this[index];
			}

			set {
				if (!ObjectIsT(value))
					throw new ArgumentException("Item is not of the correct type.", "value");
				this[index] = (T)value;
			}
		}

		void System.Collections.ICollection.CopyTo(Array array, int index) {
			if (array == null)
				throw new ArgumentNullException("array", "Destination array cannot be null.");
			CheckRangeArguments(array.Length, index, Count);

			for (int i = 0; i != Count; ++i) {
				try {
					array.SetValue(this[i], index + i);
				} catch (InvalidCastException ex) {
					throw new ArgumentException("Destination array is of incorrect type.", ex);
				}
			}
		}

		bool System.Collections.ICollection.IsSynchronized {
			get { return false; }
		}

		object System.Collections.ICollection.SyncRoot {
			get { return this; }
		}


		private static void CheckNewIndexArgument(int sourceLength, int index) {
			if (index < 0 || index > sourceLength) {
				throw new ArgumentOutOfRangeException("index", "Invalid new index " + index + " for source length " + sourceLength);
			}
		}

		private static void CheckExistingIndexArgument(int sourceLength, int index) {
			if (index < 0 || index >= sourceLength) {
				throw new ArgumentOutOfRangeException("index", "Invalid existing index " + index + " for source length " + sourceLength);
			}
		}

		private static void CheckRangeArguments(int sourceLength, int offset, int count) {
			if (offset < 0) {
				throw new ArgumentOutOfRangeException("offset", "Invalid offset " + offset);
			}

			if (count < 0) {
				throw new ArgumentOutOfRangeException("count", "Invalid count " + count);
			}

			if (sourceLength - offset < count) {
				throw new ArgumentException("Invalid offset (" + offset + ") or count + (" + count + ") for source length " + sourceLength);
			}
		}


		private bool IsEmpty {
			get { return Count == 0; }
		}

		private bool IsFull {
			get { return Count == Capacity; }
		}

		private bool IsSplit {
			get {
				// Overflow-safe version of "(offset + Count) > Capacity"
				return offset > (Capacity - Count);
			}
		}

		public int Capacity {
			get {
				return buffer.Length;
			}

			set {
				if (value < 1)
					throw new ArgumentOutOfRangeException("value", "Capacity must be greater than 0.");

				if (value < Count)
					throw new InvalidOperationException("Capacity cannot be set to a value less than Count");

				if (value == buffer.Length)
					return;

				// Create the new buffer and copy our existing range.
				T[] newBuffer = new T[value];
				if (IsSplit) {
					// The existing buffer is split, so we have to copy it in parts
					int length = Capacity - offset;
					Array.Copy(buffer, offset, newBuffer, 0, length);
					Array.Copy(buffer, 0, newBuffer, length, Count - length);
				} else {
					// The existing buffer is whole
					Array.Copy(buffer, offset, newBuffer, 0, Count);
				}

				// Set up to use the new buffer.
				buffer = newBuffer;
				offset = 0;
			}
		}

		public int Count { get; private set; }

		private int DequeIndexToBufferIndex(int index) {
			return (index + offset) % Capacity;
		}

		private T DoGetItem(int index) {
			return buffer[DequeIndexToBufferIndex(index)];
		}

		private void DoSetItem(int index, T item) {
			buffer[DequeIndexToBufferIndex(index)] = item;
		}

		private void DoInsert(int index, T item) {
			EnsureCapacityForOneElement();

			if (index == 0) {
				DoAddToFront(item);
				return;
			} else if (index == Count) {
				DoAddToBack(item);
				return;
			}

			DoInsertRange(index, new[] { item }, 1);
		}

		private void DoRemoveAt(int index) {
			if (index == 0) {
				DoRemoveFromFront();
				return;
			} else if (index == Count - 1) {
				DoRemoveFromBack();
				return;
			}

			DoRemoveRange(index, 1);
		}

		private int PostIncrement(int value) {
			int ret = offset;
			offset += value;
			offset %= Capacity;
			return ret;
		}

		private int PreDecrement(int value) {
			offset -= value;
			if (offset < 0)
				offset += Capacity;
			return offset;
		}

		private void DoAddToBack(T value) {
			buffer[DequeIndexToBufferIndex(Count)] = value;
			++Count;
		}

		private void DoAddToFront(T value) {
			buffer[PreDecrement(1)] = value;
			++Count;
		}

		private T DoRemoveFromBack() {
			T ret = buffer[DequeIndexToBufferIndex(Count - 1)];
			--Count;
			return ret;
		}

		private T DoRemoveFromFront() {
			--Count;
			return buffer[PostIncrement(1)];
		}

		private void DoInsertRange(int index, IEnumerable<T> collection, int collectionCount) {
			// Make room in the existing list
			if (index < Count / 2) {
				// Inserting into the first half of the list

				// Move lower items down: [0, index) -> [Capacity - collectionCount, Capacity - collectionCount + index)
				// This clears out the low "index" number of items, moving them "collectionCount" places down;
				//   after rotation, there will be a "collectionCount"-sized hole at "index".
				int copyCount = index;
				int writeIndex = Capacity - collectionCount;
				for (int j = 0; j != copyCount; ++j)
					buffer[DequeIndexToBufferIndex(writeIndex + j)] = buffer[DequeIndexToBufferIndex(j)];

				// Rotate to the new view
				this.PreDecrement(collectionCount);
			} else {
				// Inserting into the second half of the list

				// Move higher items up: [index, count) -> [index + collectionCount, collectionCount + count)
				int copyCount = Count - index;
				int writeIndex = index + collectionCount;
				for (int j = copyCount - 1; j != -1; --j)
					buffer[DequeIndexToBufferIndex(writeIndex + j)] = buffer[DequeIndexToBufferIndex(index + j)];
			}

			// Copy new items into place
			int i = index;
			foreach (T item in collection) {
				buffer[DequeIndexToBufferIndex(i)] = item;
				++i;
			}

			// Adjust valid count
			Count += collectionCount;
		}

		private void DoRemoveRange(int index, int collectionCount) {
			if (index == 0) {
				// Removing from the beginning: rotate to the new view
				this.PostIncrement(collectionCount);
				Count -= collectionCount;
				return;
			} else if (index == Count - collectionCount) {
				// Removing from the ending: trim the existing view
				Count -= collectionCount;
				return;
			}

			if ((index + (collectionCount / 2)) < Count / 2) {
				// Removing from first half of list

				// Move lower items up: [0, index) -> [collectionCount, collectionCount + index)
				int copyCount = index;
				int writeIndex = collectionCount;
				for (int j = copyCount - 1; j != -1; --j)
					buffer[DequeIndexToBufferIndex(writeIndex + j)] = buffer[DequeIndexToBufferIndex(j)];

				// Rotate to new view
				this.PostIncrement(collectionCount);
			} else {
				// Removing from second half of list

				// Move higher items down: [index + collectionCount, count) -> [index, count - collectionCount)
				int copyCount = Count - collectionCount - index;
				int readIndex = index + collectionCount;
				for (int j = 0; j != copyCount; ++j)
					buffer[DequeIndexToBufferIndex(index + j)] = buffer[DequeIndexToBufferIndex(readIndex + j)];
			}

			// Adjust valid count
			Count -= collectionCount;
		}

		private void EnsureCapacityForOneElement() {
			if (this.IsFull) {
				this.Capacity = this.Capacity * 2;
			}
		}

		public void AddToBack(T value) {
			EnsureCapacityForOneElement();
			DoAddToBack(value);
		}

		public void AddToFront(T value) {
			EnsureCapacityForOneElement();
			DoAddToFront(value);
		}

		public void InsertRange(int index, IEnumerable<T> collection) {
			int collectionCount = collection.Count();
			CheckNewIndexArgument(Count, index);

			// Overflow-safe check for "this.Count + collectionCount > this.Capacity"
			if (collectionCount > Capacity - Count) {
				this.Capacity = checked(Count + collectionCount);
			}

			if (collectionCount == 0) {
				return;
			}

			this.DoInsertRange(index, collection, collectionCount);
		}

		public void RemoveRange(int offset, int count) {
			CheckRangeArguments(Count, offset, count);

			if (count == 0) {
				return;
			}

			this.DoRemoveRange(offset, count);
		}

		public T RemoveFromBack() {
			if (this.IsEmpty)
				throw new InvalidOperationException("The deque is empty.");

			return this.DoRemoveFromBack();
		}

		public T RemoveFromFront() {
			if (this.IsEmpty)
				throw new InvalidOperationException("The deque is empty.");

			return this.DoRemoveFromFront();
		}

		public void Clear() {
			this.offset = 0;
			this.Count = 0;
		}

		[DebuggerNonUserCode]
		private sealed class DebugView {
			private readonly Deque<T> deque;

			public DebugView(Deque<T> deque) {
				this.deque = deque;
			}

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public T[] Items {
				get {
					var array = new T[deque.Count];
					((ICollection<T>)deque).CopyTo(array, 0);
					return array;
				}
			}
		}
	}
}
