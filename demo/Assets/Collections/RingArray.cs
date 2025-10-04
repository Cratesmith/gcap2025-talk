using System;
using System.Collections;
using System.Collections.Generic;

namespace Collections
{
	public class RingArray<T> : IList<T> 
	{
		public T[] Buffer { get; private set; }
		public int Head { get; private set; }
		public int Tail { get; private set; }
		public int Modifcation { get; private set; }

		public int Count {
			get
			{
				if (Head == -1)
				{
					return 0;
				}

				return Tail < Head
					? Buffer.Length - Head + Tail + 1
					: Tail - Head + 1;
			}
		}

		public int Capacity => Buffer.Length;

		public int MaxCapacity { get; private set; }

		public RingArray(int capacity = 16, int maxCapacity = -1)
		{
			Buffer = new T[capacity];
			MaxCapacity = maxCapacity;
			Clear();
		}
		
		public ref T InsertAt(int index, in T value)
		{
			++Modifcation;
			
			if (index==Count)
			{
				return ref Add(value);
			}

			if (Count == Capacity)
			{
				ResizeCapacity(Capacity*2);
			}
			
			var insertAt = ListToBufferIndex(index);
			Tail = Tail == Buffer.Length - 1 ? 0 : Tail + 1;
			var i = Tail;
			do
			{
				var prev = i;
				i = i>0 ? i-1 : Buffer.Length-1;
				Buffer[prev] = Buffer[i];
			} while (i != insertAt);
			Buffer[insertAt] = value;

			return ref Buffer[insertAt];
		}
		
		public ref T Add(in T value)
		{
			++Modifcation;
			
			if (Count == Buffer.Length)
			{
				ResizeCapacity(Capacity*2);
			}

			Tail = Tail == Buffer.Length - 1 ? 0 : Tail + 1;
			Buffer[Tail] = value;

			if (Head == -1)
			{
				Head = Tail;
			}
			
			return ref Buffer[Tail];
		}
		
		public void Append(in T[] other)
		{
			if (other.Length <= 0)
			{
				return;
			}

			++Modifcation;
			
			if (Count+other.Length > Buffer.Length)
			{
				ResizeCapacity(Math.Max(Count+other.Length, Capacity*2));
			}
			
			var copyFrom = Tail == Buffer.Length - 1 ? 0 : Tail + 1;
			var copyCount = Math.Min(other.Length, Buffer.Length - copyFrom);
			Array.ConstrainedCopy(other, 0, Buffer, copyFrom, copyCount);
			Head = Head == -1 ? copyFrom : Head;
			Tail = copyFrom + copyCount - 1;

			var remain = other.Length - copyCount;
			if (remain > 0)
			{
				Array.ConstrainedCopy(other, copyCount, Buffer, 0, remain);
				Tail = remain - 1;
			}
			
		}
		
		public void Append<TCollection>(in TCollection other) where TCollection:ICollection<T>
		{
			if (other.Count <= 0)
			{
				return;
			}

			++Modifcation;
			
			if (Count+other.Count >= Buffer.Length)
			{
				ResizeCapacity(Math.Max(Count+other.Count, Capacity*2));
			}

			using var iter = other.GetEnumerator();
			while (iter.MoveNext())
			{
				Add(iter.Current);
			}
		}
		
		public void Append(in RingArray<T> other)  
		{
			++Modifcation;
			
			if (Count+other.Count >= Buffer.Length)
			{
				ResizeCapacity(Math.Max(Count+other.Count, Capacity*2));
			}

			foreach (ref T value in other)
			{
				Add(value);
			}
		}

		public ref T InsertSorted(in T value, InComparison<T> comparison, bool itemAreSorted)
		{
			++Modifcation;
			
			if (Head == -1)
			{
				return ref Add(value);
			}
			
			if (Count == Buffer.Length)
			{
				ResizeCapacity(Capacity * 2);
			}

			// if existing items are sorted we can use a binary search
			if (itemAreSorted)
			{
				var l = 0;
				var h = Count;	
				var insertAt = Count/2;

				while (l < h)
				{
					insertAt = (h - l) / 2 + l;
					var result = comparison(value, this[insertAt]);

					if (result>0)
						insertAt = l = insertAt+1;
					else if (result<0)
						insertAt = h = insertAt;
					else
						l = h = insertAt;
				}

				return ref Buffer[insertAt];
			}
			
			// otherwise do linear search
			var current = Head-1;
			do
			{
				current = current != Buffer.Length ? current + 1 : 0;
				if (comparison(value, Buffer[current]) < 0)
				{
					var index = current >= Head
						? current - Head
						: Buffer.Length - Head + current;

					return ref InsertAt(index, value);
				}
			} while (current!=Tail);

			return ref Add(value);
		}

		public bool TryResizeCapacity(int minCapacity)
		{
			if (MaxCapacity != -1 && minCapacity > MaxCapacity)
				return false;
			
			ResizeCapacity(minCapacity);
			return true;
		}
		
		public void ResizeCapacity(int minCapacity)
		{
			if (MaxCapacity != -1 && minCapacity > MaxCapacity)
			{
				throw new ArgumentException($"Unable to grow capacity to {minCapacity}, Max capacity:{MaxCapacity}");
			}

			T[] newBuffer =  new T[minCapacity];
			if (Head >= 0)
			{
				int count = Count;
				if (Tail >= Head)
				{
					Array.ConstrainedCopy(Buffer, Head,  newBuffer, 0, Tail-Head);
				} else
				{
					var split = Buffer.Length - Head;
					Array.ConstrainedCopy(Buffer, Head,  newBuffer, 0, split);
					Array.ConstrainedCopy(Buffer, 0,  newBuffer, split, Tail+1);
				}

				Buffer = newBuffer;
				Head = 0;
				Tail = count-1;
			}
		}
		
		public ref T this[int index] => ref Buffer[ListToBufferIndex(index)];

		int BufferToListIndex(int index)
		{
			var result = index >= Head 
				? index-Head
				: Buffer.Length-Head + index;
			
			if (result < 0 || result >= Count)
			{
				throw new ArgumentOutOfRangeException("index", $"At called with out of range index:{index} count:{Count}.");
			}

			return result;
		}
		
		int ListToBufferIndex(int index)
		{
			if (index < 0 || index >= Count)
			{
				throw new ArgumentOutOfRangeException("index", $"At called with out of range index:{index} count:{Count}.");
			}

			index += Head;

			if (index >= Buffer.Length)
			{
				index -= Buffer.Length;
			}

			return index;
		}

		public int ListIndexOf(in T other)
		{
			var bufferIndex = BufferIndexOf(other);
			return bufferIndex!=-1 ? BufferToListIndex(bufferIndex):-1;
		}

		public int BufferIndexOf(in T other) 
		{
			if (Head>=0)
			{
				int i = Head-1;
				do
				{
					i = i == Buffer.Length - 1 ? 0 : i + 1;
					if(other.Equals(Buffer[i]))
					{
						return i;
					}
				} while (i != Tail);
			}
			return -1;
		}
		
		public int ListIndexOf<T2>(in T2 other) where T2 : IEquatable<T> => BufferToListIndex(BufferIndexOf(other));

		public int BufferIndexOf<T2>(in T2 other) where T2 : IEquatable<T>
		{
			if (Head>=0)
			{
				int i = Head-1;
				do
				{
					i = i == Buffer.Length - 1 ? 0 : i + 1;
					if(other.Equals(Buffer[i]))
					{
						return i;
					}
				} while (i != Tail);
			}
			return -1;
		}

		public bool Remove(in T other, bool copyLastToReplace = false) 
		{
			var index = ListIndexOf(other);
			if (index != -1)
			{
				RemoveAt(index, copyLastToReplace);
				return true;
			}
			return false;
		}
		
		public bool Remove<T2>(in T2 other, bool copyLastToReplace = false) where T2 : IEquatable<T>
		{
			var index = ListIndexOf(other);
			if (index != -1)
			{
				RemoveAt(index, copyLastToReplace);
				return true;
			}
			return false;
		}

		public void RemoveAt(int index, bool copyLastToReplace = false, bool wipeRemoved = false)
		{
			int i = ListToBufferIndex(index);
			int wipe = -1;
			
			if (copyLastToReplace)
			{
				wipe = Tail;
				
				if (i == Tail)
				{
					if (i == Head)
					{
						Head = -1;
						Tail = -1;
					}
				}
				else
				{
					Buffer[i] = Buffer[Tail];
				}

				if (Tail != -1)
				{
					Tail = Tail == 0 ? Buffer.Length - 1 : Tail - 1;
				}
			} else
			{
				if (i == Head)
				{
					if (i == Tail)
					{
						Head = -1;
						Tail = -1;
					} else
					{
						wipe = Head;
						Head = Head == Buffer.Length - 1 ? 0 : Head + 1;
					}
				} else
				{
					while (i != Tail)
					{
						int next = i + 1 != Buffer.Length
							? i + 1
							: 0;

						Buffer[i] = Buffer[next];
						i = next;
					}

					Tail = Tail == 0 ? Buffer.Length - 1 : Tail - 1;
				}
			}
			
			if (wipeRemoved && wipe != -1)
			{
				Buffer[wipe] = default;
			}
		}
		
		public void Clear(bool wipeRemoved = false)
		{
			if (wipeRemoved)
			{
				using var iter = GetEnumerator();
				while (iter.MoveNext())
				{
					iter.Current = default;
				}
			}
			Head = -1;
			Tail = -1;
		}
		
		public struct Enumerator : IEnumerator<T>
		{
			public int Index { get; private set; }
			int              m_NextIndex;
			RingArray<T> m_Buffer;

			public Enumerator(RingArray<T> buffer)
			{
				Index = -1;
				m_NextIndex = 0;
				m_Buffer = buffer;
			}
			
			public Enumerator(RingArray<T> buffer, int index)
			{
				Index = index;
				m_NextIndex = index+1;
				m_Buffer = buffer;
			}

			public bool MoveNext()
			{
				if (m_NextIndex >= m_Buffer.Count)
				{
					return false;
				}

				Index = m_NextIndex;
				++m_NextIndex;
				return true;
			}

			public void Reset()
			{
				Index = -1;
				m_NextIndex = 0;
			}

			public ref T Current => ref m_Buffer[Index];

			public bool Valid => Index >= 0;

			public void Remove(bool copyLastToReplace = false)
			{
				m_Buffer.RemoveAt(Index, copyLastToReplace);
				m_NextIndex = Index;
				Index = -1;
			}

			public T Consume()
			{
				T copy = m_Buffer[Index];
				Remove();
				return copy;
			}

			T IEnumerator<T>.Current => Current;
			object IEnumerator.Current => Current;
			public void Dispose()
			{
				m_Buffer = null;
			}
		}
		
		public Enumerator GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		public ref T GetFirst()
		{
			return ref Buffer[Head];
		}
		
		public ref T GetLast()
		{
			return ref Buffer[Tail];
		}
		
		public bool Find<TContext>(in TContext context, ContextPredicate<TContext, T> predicate, out Enumerator output)
		{
			using var iter = GetEnumerator();
			while (iter.MoveNext())
			{
				if (predicate(context, iter.Current))
				{
					output = iter;
					return true;
				}
			}

			output = default;
			return false;
		}
		
		/// <summary>
		/// Find an item by binary search. All items must be sorted for this to work 
		/// </summary>
		public bool BinarySearch<TContext>(in TContext context, InMultiComparison<TContext, T> comparison, out Enumerator output)
		{
			if (Head != -1)
			{
				var l = 0;
				var h = Count;
				while (l < h)
				{
					var current = (h - l) / 2 + l;
					var result = comparison(context, this[current]);

					if (result > 0)
						l = current + 1;
					else if (result < 0)
						h = current;
					else
					{
						output = new Enumerator(this, current);
						return true;
					}
				}
			}

			output = default;
			return false;
		}

		public bool RemoveFirst<TContext>(in TContext context, ContextPredicate<TContext,T> predicate, bool copyLastToReplace = false)
		{
			using var iter = GetEnumerator();
			while(iter.MoveNext())
			{
				if (predicate(context, iter.Current))
				{
					iter.Remove(copyLastToReplace);
					return true;
				}
			}

			return false;
		}
		
		public bool RemoveFirstSorted<TContext>(in TContext context, InMultiComparison<TContext, T> comparison, bool copyLastToReplace = false)
		{
			if (Head != -1)
			{
				var l = 0;
				var h = Count;

				while (l < h)
				{
					var current = (h - l) / 2 + l;
					var result = comparison(context, this[current]);

					if (result > 0)
						l = current + 1;
					else if (result < 0)
						h = current;
					else
					{
						RemoveAt(current, copyLastToReplace);
						return true;
					}
				}
			}

			return false;
		}

		public int RemoveAll<TContext>(in TContext context, ContextPredicate<TContext, T> predicate, bool copyLastToReplace = false)
		{
			int count = 0;

			using var  iter = GetEnumerator();
			while(iter.MoveNext())
			{
				if (predicate(context, iter.Current))
				{
					iter.Remove(copyLastToReplace);
					++count;
				}
			}

			return count;
		}
		
		
		public void RemoveRange(int start, int count, bool copyLastToReplace = false, bool wipeRemoved = false)
		{
			if (count == 0 || start < 0 || start > Count)
			{
				return;
			}
			
			for (int i = start+count-1; i>=start; --i)
			{
				RemoveAt(i, copyLastToReplace, wipeRemoved);
			}
		}

		int IList<T>.IndexOf(T item) => ListIndexOf(item);
		void IList<T>.Insert(int index, T item) => InsertAt(index, item);
		void IList<T>.RemoveAt(int index) => RemoveAt(index);
		T IList<T>.this[int index] { get=>this[index]; set=>this[index]=value; }
		void ICollection<T>.Clear() => Clear(); 
		bool ICollection<T>.Remove(T item) => Remove(item);
		bool ICollection<T>.IsReadOnly => false;
		void ICollection<T>.Add(T item) => Add(item);
		bool ICollection<T>.Contains(T item) => BufferIndexOf(item)!=-1;
		void ICollection<T>.CopyTo(T[] array, int arrayIndex)
		{
			for (int i = arrayIndex; i < array.Length; i++)
			{
				array[i] = this[i];
			}
		}
	}
	
	public delegate bool ContextPredicate<TContext, T>(in TContext context, in T value);
	public delegate int ContextComparison<TContext, T>(in TContext context, in T a, in T b);
	public delegate int InComparison<T>(in T a, in T b);
	public delegate int InMultiComparison<T1,T2>(in T1 a, in T2 b);
}


