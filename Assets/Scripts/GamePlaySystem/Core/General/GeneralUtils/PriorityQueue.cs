// // Copyright 2017-2021 StagPoint Software
//
// namespace SparFlame.GamePlaySystem.General
// {
// 	using System;
// 	using System.Diagnostics;
// 	using System.Runtime.CompilerServices;
//
// 	using Unity.Collections;
// 	using Unity.Collections.LowLevel.Unsafe;
// 	// TODO What is unsafe code and can i use it
// 	/// <summary>
// 	/// Priority Queue implementation with item data stored in native containers. 
// 	/// </summary>
// 	/// <typeparam name="T"></typeparam>
// 	[NativeContainer]
// 	[DebuggerDisplay( "Length = {Length}" )]
// 	[DebuggerTypeProxy( typeof( NativePriorityQueueDebugView<> ) )]
// 	public unsafe struct NativePriorityQueue<T> : IDisposable 
// 		where T : struct, IComparable<T>
// 	{
// 		#region Public properties
//
// 		/// <summary>
// 		/// Returns the number of values stored in the queue
// 		/// </summary>
// 		public int Length
// 		{
// 			[MethodImpl( MethodImplOptions.AggressiveInlining )]
// 			get { return _mListData != null ? _mListData->Length : 0; }
// 		}
//
// 		/// <summary>
// 		/// Returns true if the queue is empty
// 		/// </summary>
// 		public bool IsEmpty
// 		{
// 			[MethodImpl( MethodImplOptions.AggressiveInlining )]
// 			get { return _mListData == null || _mListData->Length == 0; }
// 		}
//
// 		#endregion
//
// 		#region Private fields
//
// 		private const int DefaultSize = 32;
// 		private const int GrowthFactor = 2;
//
// 		private Allocator _mAllocatorLabel;
// 		private NativeArray<T> _mBuffer;
//
// 		[NativeDisableUnsafePtrRestriction]
// 		private UnsafeListData* _mListData;
//
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
// 		internal AtomicSafetyHandle MSafety;
// 		[NativeSetClassTypeToNullOnSchedule] internal DisposeSentinel MDisposeSentinel;
// #endif	
// 		
// 		#endregion
//
// 		#region Constructor
//
// 		/// <summary>
// 		/// Initializes a new instance of the BinaryHeap class with a the indicated capacity
// 		/// </summary>
// 		public NativePriorityQueue( int capacity = DefaultSize, Allocator allocator = Allocator.TempJob )
// 		{
// 			if( capacity < 1 )
// 			{
// 				throw new ArgumentException( "Capacity must be greater than zero" );
// 			}
//
// 			_mAllocatorLabel = allocator;
//
// 			_mListData = (UnsafeListData*)UnsafeUtility.Malloc( UnsafeUtility.SizeOf<UnsafeListData>(), UnsafeUtility.AlignOf<UnsafeListData>(), allocator );
// 			_mBuffer = new NativeArray<T>( capacity, allocator, NativeArrayOptions.UninitializedMemory );
//
// 			_mListData->Capacity = capacity;
// 			_mListData->Length = 0;
//
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
// 			DisposeSentinel.Create( out MSafety, out MDisposeSentinel, 0, allocator );
// #endif
//
// 		}
//
// 		#endregion
//
// 		#region Public methods
//
// 		/// <summary>
// 		/// Disposes of any native memory held by this instance
// 		/// </summary>
// 		public void Dispose()
// 		{
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
// 			DisposeSentinel.Dispose( ref MSafety, ref MDisposeSentinel );
// # endif
// 			
// 			if( !_mBuffer.IsCreated )
// 			{
// 				throw new InvalidOperationException( $"This collection has already been disposed" );
// 			}
//
// 			UnsafeUtility.Free( _mListData, _mAllocatorLabel );
// 			_mBuffer.Dispose();
//
// 			_mListData = null;
// 			_mAllocatorLabel = Allocator.Invalid;
// 		}
//
// 		/// <summary>
// 		/// Removes all items from the heap.
// 		/// </summary>
// 		[MethodImpl( MethodImplOptions.AggressiveInlining )]
// 		public void Clear()
// 		{
// 			_mListData->Length = 0;
// 			// m_Buffer.Clear();
// 			for (var i = 0; i < _mBuffer.Length; i++)
// 			{
// 				_mBuffer[i] = default;
// 			}
// 			// UnsafeUtility.MemClear(m_Buffer.GetUnsafePtr(), UnsafeUtility.SizeOf<T>() * m_Buffer.Length);
// 		}
//
// 		/// <summary>
// 		/// Returns the first item in the heap without removing it from the heap
// 		/// </summary>
// 		/// <returns></returns>
// 		[MethodImpl( MethodImplOptions.AggressiveInlining )]
// 		public T Peek()
// 		{
// 			if( _mListData->Length == 0 )
// 			{
// 				throw new InvalidOperationException( "Cannot peek at first item when the heap is empty." );
// 			}
//
// 			return _mBuffer[ 0 ];
// 		}
//
// 		/// <summary>
// 		/// Adds a key and value to the heap.
// 		/// </summary>
// 		/// <param name="item">The item to add to the heap.</param>
// 		[MethodImpl( MethodImplOptions.AggressiveInlining )]
// 		public void Enqueue( T item )
// 		{
// 			if( _mListData->Length == _mListData->Capacity )
// 			{
// 				EnsureCapacity( _mListData->Length + 1 );
// 			}
//
// 			_mBuffer[ _mListData->Length ] = item;
//
// 			HeapIfUp( _mListData->Length, item );
//
// 			_mListData->Length++;
// 		}
//
// 		/// <summary>
// 		/// Removes and returns the first item in the heap.
// 		/// </summary>
// 		/// <returns>The first value in the heap.</returns>
// 		public T Dequeue()
// 		{
// 			if( _mListData->Length == 0 )
// 			{
// 				throw new InvalidOperationException( "Cannot remove item from an empty heap" );
// 			}
//
// 			// Stores the key of root node to be returned
// 			var v = _mBuffer[ 0 ];
//
// 			// Decrease heap size by 1
// 			_mListData->Length -= 1;
//
// 			// Copy the last node to the root node and clear the last node
// 			_mBuffer[ 0 ] = _mBuffer[ _mListData->Length ];
// 			_mBuffer[ _mListData->Length ] = default( T );
//
// 			// Restore the heap property of the tree
// 			HeapIfDown( 0, _mBuffer[ 0 ] );
//
// 			return v;
// 		}
//
// 		/// <summary>
// 		/// Ensures that there is large enough internal capacity to store the indicated number of
// 		/// items without having to re-allocate the internal buffer.
// 		/// </summary>
// 		/// <param name="count"></param>
// 		public void EnsureCapacity( int count )
// 		{
// 			var originalLength = _mListData->Capacity;
// 			while( count > _mListData->Capacity )
// 			{
// 				_mListData->Capacity *= GrowthFactor;
// 			}
//
// 			// Create a new array to hold the item data and copy the existing data into it. 
// 			var dataSize = UnsafeUtility.SizeOf<T>() * originalLength;
// 			var newArray = new NativeArray<T>( _mListData->Capacity, _mAllocatorLabel );
// 			UnsafeUtility.MemCpy( _mBuffer.GetUnsafePtr(), newArray.GetUnsafePtr(), dataSize );
//
// 			// Dispose of the existing array 
// 			_mBuffer.Dispose();
//
// 			// The new array is now this instance's items array 
// 			_mBuffer = newArray;
// 		}
//
// 		/// <summary>
// 		/// Returns the raw (not necessarily sorted) contents of the priority queue as a managed array.
// 		/// </summary>
// 		/// <returns></returns>
// 		public T[] ToArray()
// 		{
// 			var length = _mListData->Length;
//
// 			T[] result = new T[ length ];
// 			for( int i = 0; i < length; i++ )
// 			{
// 				result[ i ] = _mBuffer[ i ];
// 			}
//
// 			return result;
// 		}
//
// 		#endregion
//
// 		#region Private utility methods
//
// 		private int HeapIfUp( int index, T item )
// 		{
// 			var parent = ( index - 1 ) >> 1;
//
// 			while( parent > -1 && item.CompareTo( _mBuffer[ parent ] ) <= 0 )
// 			{
// 				// Swap nodes
// 				_mBuffer[ index ] = _mBuffer[ parent ];
//
// 				index = parent;
// 				parent = ( index - 1 ) >> 1;
// 			}
//
// 			_mBuffer[ index ] = item;
//
// 			return index;
// 		}
//
// 		private int HeapIfDown( int parent, T item )
// 		{
// 			while( true )
// 			{
// 				var index = 0;
//
// 				var ch1 = ( parent << 1 ) + 1;
// 				if( ch1 >= _mListData->Length )
// 					break;
// 				var ch2 = ( parent << 1 ) + 2;
// 				if( ch2 >= _mListData->Length )
// 				{
// 					index = ch1;
// 				}
// 				else
// 				{
// 					index = _mBuffer[ ch1 ].CompareTo( _mBuffer[ ch2 ] ) <= 0 ? ch1 : ch2;
// 				}
//
// 				if( item.CompareTo( _mBuffer[ index ] ) < 0 )
// 					break;
//
// 				_mBuffer[ parent ] = _mBuffer[ index ]; // Swap nodes
// 				parent = index;
// 			}
//
// 			_mBuffer[ parent ] = item;
//
// 			return parent;
// 		}
// 		#endregion
//
// 		#region Debugging support
//
// 		public override string ToString()
// 		{
// 			return string.Format( "Length={0}", _mListData != null ? _mListData->Length : -1 );
// 		}
//
// 		#endregion
//
// 	}
//
// 	#region Related types 
//
// 	public struct UnsafeListData
// 	{
// 		public int Length;
// 		public int Capacity;
// 	}
//
// 	internal sealed class NativePriorityQueueDebugView<T>
// 		where T : struct, IComparable<T>
// 	{
// 		private NativePriorityQueue<T> _list;
//
// 		/// <summary>
// 		/// Create the view for a given list
// 		/// </summary>
// 		/// 
// 		/// <param name="list">
// 		/// List to view
// 		/// </param>
// 		public NativePriorityQueueDebugView( NativePriorityQueue<T> list )
// 		{
// 			this._list = list;
// 		}
//
// 		/// <summary>
// 		/// Get a managed array version of the list's elements to be viewed in the
// 		/// debugger.
// 		/// </summary>
// 		public T[] Items
// 		{
// 			get
// 			{
// 				return _list.ToArray();
// 			}
// 		}
// 	}
//
// 	#endregion
// }