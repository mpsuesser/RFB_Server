using System.Collections;
using System;
using UnityEngine;

public class Heap<T> where T : IHeapItem<T> {
    T[] items;
    int currentItemCount;

    public Heap(int maxHeapSize) {
        items = new T[maxHeapSize];
        currentItemCount = 0;
    }

    public void Add(T item) {
        item.HeapIndex = currentItemCount;
        items[currentItemCount] = item;
        SortUp(item);
        currentItemCount++;
    }

    public T RemoveFirst() {
        T firstItem = items[0];
        currentItemCount--;
        items[0] = items[currentItemCount];
        items[0].HeapIndex = 0;
        SortDown(items[0]);
        return firstItem;
    }

    public void UpdateItem(T item) {
        SortUp(item);
    }

    public int Count => currentItemCount;

    public bool Contains(T item) {
        return Equals(items[item.HeapIndex], item);
    }

    private void SortDown(T item) {
        while (true) {
            int childIndexLeft = item.HeapIndex * 2 + 1;
            int childIndexRight = item.HeapIndex * 2 + 2;
            int swapIndex = 0;

            if (childIndexLeft < currentItemCount) {
                swapIndex = childIndexLeft;

                if (childIndexRight < currentItemCount) {
                    if (items[childIndexLeft].CompareTo(items[childIndexRight]) < 0) {
                        swapIndex = childIndexRight;
                    }
                }

                if (item.CompareTo(items[swapIndex]) < 0) {
                    Swap(item, items[swapIndex]);
                } else {
                    return;
                }
            } else {
                return;
            }
        }
    }

    /*  Index of any node's parent: nodeIndex - 1 / 2
     *  Index of any node's left child: 2 * nodeIndex + 1
     *  Index of any node's right child: 2 * nodeIndex + 2
     */
    private void SortUp(T item) {
        int parentIndex = (item.HeapIndex - 1) / 2;

        while (true) {
            T parentItem = items[parentIndex];
            if (item.CompareTo(parentItem) > 0) {
                Swap(item, parentItem);
            } else {
                break;
            }

            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }

    private void Swap(T _A, T _B) {
        items[_A.HeapIndex] = _B;
        items[_B.HeapIndex] = _A;

        int originalA = _A.HeapIndex;
        _A.HeapIndex = _B.HeapIndex;
        _B.HeapIndex = originalA;
    }
}

public interface IHeapItem<T> : IComparable<T> {
    int HeapIndex {
        get;
        set;
    }
}
