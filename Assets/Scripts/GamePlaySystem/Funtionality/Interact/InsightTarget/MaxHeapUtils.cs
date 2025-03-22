using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
// ReSharper disable UseIndexFromEndExpression


namespace SparFlame.GamePlaySystem.Interact
{
    public struct MaxHeapUtils<T> where T : unmanaged,IComparable<T>,IEntityContained
    {
        
        // TODO : Add sentinel search support
        // TODO : Change to index max priority map to reduce remove times

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static void Enqueue( T item ,ref DynamicBuffer<T> buffer)
        {
            buffer.Add(item);
            HeapIfUp( buffer.Length - 1, ref item,ref buffer );
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static Entity GetMax(ref DynamicBuffer<T> buffer)
        {
            return buffer[0].Entity;
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static Entity GetMin(ref DynamicBuffer<T> buffer)
        {
            return buffer[buffer.Length - 1].Entity;
        }
        
        public static void RebuildHeapAfterWholeUpdate(ref DynamicBuffer<T> buffer)
        {
            if (buffer.Length <= 1) return;
            for (var i = buffer.Length / 2 - 1; i >= 0; i--)
            {
                var item = buffer[i];
                HeapIfDown(i, ref item, ref buffer);
            }
        }

        
        public static void TryRemove(ref DynamicBuffer<T> buffer,Entity targetEntity)
        {
            int i;
            for ( i = 0; i<buffer.Length; i++)
            {
                if(targetEntity == buffer[i].Entity)break;
            }
            if(i>= buffer.Length)return;
            buffer.ElementAt(i) = buffer[buffer.Length - 1];
            buffer.RemoveAt(buffer.Length - 1);
            
            HeapIfDown(i, ref buffer.ElementAt(i), ref buffer );
        }
        
        public static T Dequeue(ref DynamicBuffer<T> buffer)
        {
            if( buffer.Length == 0 )
            {
                throw new InvalidOperationException( "Cannot remove item from an empty heap" );
            }
            // Stores the key of root node to be returned
            var v = buffer[ 0 ];
            
            // Copy the last node to the root node and clear the last node
            buffer.ElementAt(0) = buffer[ buffer.Length-1 ];
            buffer.RemoveAt(buffer.Length-1);
            
            // Restore the heap property of the tree
            HeapIfDown( 0,ref buffer.ElementAt(0),ref buffer );
            return v;
        }
        
        
        private static int HeapIfUp( int index,ref T item ,ref DynamicBuffer<T> buffer)
        {
            var parent = ( index - 1 ) >> 1;
            while( parent > -1 && item.CompareTo( buffer[ parent ] )>= 0 )
            {
                // Swap nodes
                buffer.ElementAt(index) = buffer[ parent ];
                index = parent;
                
                parent = ( index - 1 ) >> 1;
            }
            buffer.ElementAt(index) = item;
            return index;
        }


        private static int HeapIfDown(int parent,ref T item, ref DynamicBuffer<T> buffer)
        {
            while (true)
            {
                var index = 0;

                var ch1 = (parent << 1) + 1;
                if (ch1 >= buffer.Length)
                    break;
                var ch2 = (parent << 1) + 2;
                if (ch2 >= buffer.Length)
                {
                    index = ch1;
                }
                else
                {
                    index = buffer[ch1].CompareTo(buffer[ch2]) >= 0 ? ch1 : ch2;
                }

                if (item.CompareTo(buffer[index]) >= 0)
                    break;

                buffer.ElementAt(parent) = buffer[index]; // Swap nodes
                parent = index;
            }

            buffer.ElementAt(parent) = item;
            return parent;
        }
        
       
        
    }
}