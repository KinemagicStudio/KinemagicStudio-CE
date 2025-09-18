using System;

namespace Kinemagic.AppCore.Utils
{
    public sealed class RingBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly int _indexMask;

        private int _head;
        private int _tail;

        public int Capacity => _buffer.Length;
        public int Count => _tail - _head;

        public T this[int index]
        {
            get => Peek(index);
            set
            {
                if (index < 0 || index >= Capacity)
                {
                    throw new IndexOutOfRangeException();
                }
                _buffer[(_head + index) & _indexMask] = value;
            }
        }

        public RingBuffer(int capacity)
        {
            capacity = 1 << (int)Math.Ceiling(Math.Log(capacity, 2)); // Ensure capacity is a power of two
            _buffer = new T[capacity];
            _indexMask = capacity - 1;
            _head = 0;
            _tail = 0;
        }

        public void Clear()
        {
            _head = 0;
            _tail = 0;
        }

        /// <summary>
        /// Adds an item to the tail of the buffer.
        /// If the buffer is full, the item at the head (the oldest item) is overwritten.
        /// </summary>
        public void Enqueue(T item)
        {
            _buffer[_tail & _indexMask] = item;
            _tail++;

            if (Count > _buffer.Length)
            {
                _head++;
            }
        }

        /// <summary>
        /// Adds an item to the head of the buffer.
        /// If the buffer is full, the item at the tail (the newest item) is overwritten.
        /// </summary>
        public void Push(T item)
        {
            _head--;
            _buffer[_head & _indexMask] = item;

            if (Count > _buffer.Length)
            {
                _tail--;
            }
        }

        /// <summary>
        /// Adds an item at the specified index counting from the tail of the buffer.
        /// If the buffer is full, the item at the head (the oldest item) is overwritten.
        /// </summary>
        public void InsertFromTail(int indexFromEnd, T item)
        {
            var index = Count - indexFromEnd;
            if (index < 0 || index > Count)
            {
                throw new IndexOutOfRangeException();
            }

            for (var i = Count; i > index; i--)
            {
                _buffer[(_head + i) & _indexMask] = _buffer[(_head + i - 1) & _indexMask];
            }

            _buffer[(_head + index) & _indexMask] = item;
            _tail++;

            if (Count > _buffer.Length)
            {
                _head++;
            }
        }

        /// <summary>
        /// Gets the item at the head of the buffer.
        /// </summary>
        public ref readonly T PeekHead()
        {
            if (_tail == _head)
            {
                throw new InvalidOperationException("Buffer is empty.");
            }
            return ref _buffer[_head & _indexMask];
        }

        /// <summary>
        /// Gets the item at the tail of the buffer.
        /// </summary>
        public ref readonly T PeekTail()
        {
            if (_tail == _head)
            {
                throw new InvalidOperationException("Buffer is empty.");
            }
            return ref _buffer[(_tail - 1) & _indexMask];
        }

        /// <summary>
        /// Gets the item at the specified index in the buffer.
        /// </summary>
        /// <param name="index">The index counting from the head of the buffer.</param>
        public ref readonly T Peek(int index)
        {
            if (index < 0 || index >= Capacity)
            {
                throw new IndexOutOfRangeException();
            }
            return ref _buffer[(_head + index) & _indexMask];
        }
    }
}