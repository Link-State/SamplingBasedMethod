using System;
using System.Collections.Generic;

public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
	private readonly List<(TElement element, TPriority priority)> _heap = new List<(TElement, TPriority)>();
	public int Count => _heap.Count;


	public bool Enqueue(TElement element, TPriority priority) {
		_heap.Add((element, priority));
		int index = _heap.Count - 1;

		while (index > 0) {
			int parent = (index - 1) / 2;

			if (_heap[index].priority.CompareTo(_heap[parent].priority) >= 0) {
				return true;
			}

			(TElement, TPriority) temp = _heap[index];
			_heap[index] = _heap[parent];
			_heap[parent] = temp;
			index = parent;
		}

		return false;
	}

	public bool TryDequeue(out TElement element, out TPriority priority) {
		if (_heap.Count <= 0) {
			element = default;
			priority = default;
			return false;
		}

		element = _heap[0].element;
		priority = _heap[0].priority;

		(TElement, TPriority) lastElement = _heap[^1];
		_heap[0] = lastElement;
		_heap.RemoveAt(_heap.Count - 1);
		if (((float)_heap.Capacity) / ((float)_heap.Count) >= 2f) {
			_heap.TrimExcess();
		}

		int index = 0;
		int count = _heap.Count;

		while (true) {
			int left = index * 2 + 1;
			int right = index * 2 + 2;
			int current = index;

			if (left < count && _heap[left].priority.CompareTo(_heap[current].priority) < 0) {
				current = left;
			}

			if (right < count && _heap[right].priority.CompareTo(_heap[current].priority) < 0) {
				current = right;
			}

			if (current == index) {
				return true;
			}

			(_heap[index], _heap[current]) = (_heap[current], _heap[index]);
			index = current;
		}
	}
}
