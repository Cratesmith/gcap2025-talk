using System.Collections.Generic;
using Collections;
using NUnit.Framework;
using UnityEngine;

public class RingArrayTests
{
    public struct TestItem
    {
        public int value;
    }
    
    // A Test behaves as an ordinary method
    [Test]
    public void Add()
    {
        RingArray<TestItem> buffer = new RingArray<TestItem>(16, 16);
        buffer.Add(new TestItem()
        {
            value = 1
        });
        Assert.AreEqual(buffer.Head, 0);
        Assert.AreEqual(buffer.Tail, 0);
        Assert.AreEqual(buffer.Count, 1);
        Assert.AreEqual(buffer[0].value, 1);
        Assert.AreEqual(buffer[0].value, 1);
    }

    [Test]
    public void RemoveAt()
    {
        RingArray<int> buffer = new RingArray<int>(16, 16);
        // remove only item 
        buffer.Add(0);
        Assert.AreEqual(buffer.Count, 1);
        Assert.AreEqual(buffer.Head, 0);
        Assert.AreEqual(buffer.Tail, 0);
        buffer.RemoveAt(0);
        Assert.AreEqual(buffer.Count, 0);
        Assert.AreEqual(buffer.Head, -1);
        Assert.AreEqual(buffer.Tail, -1);

        void _RemoveTest(int index, bool copyLast)
        {
            Debug.Log($"RemoveAt({index}, {copyLast})");
            
            // initialize buffer in a wrap position
            buffer.Clear();
            buffer.Add(-2);
            buffer.Add(-1);
            for (int j = 0; j < buffer.Capacity-2; j++)
            {
                buffer.Add(j);
            }
            buffer.RemoveRange(0, 2);
            for (int j = buffer.Count; j < buffer.Capacity; j++)
            {
                buffer.Add(j);
            }

            buffer.RemoveAt(index, copyLast);
            Assert.AreEqual(buffer.Count, buffer.Capacity-1);
            for (int j = 0; j < buffer.Count; ++j)
                {
                    Debug.Log($"[{j}] = {buffer[j]}");
                    switch (j.CompareTo(index))
                    {
                        case -1:
                            Assert.AreEqual(buffer[j], j, "Ealier indicies should be same");
                            break;
                        
                        case 0:
                            if(copyLast)
                                Assert.AreEqual(buffer[j], buffer.Count, "Replaced index should contain the last item");
                            else
                                Assert.AreEqual(buffer[j], j+1, "Removed index should contain next item");
                            break;
                        case 1:
                            if(copyLast)
                                Assert.AreEqual(buffer[j], j, "After replaced item, indicies should be unchanged");
                            else
                                Assert.AreEqual(buffer[j], j+1, "After removed item, all values should be shifted back one index");
                            
                            break;
                    }
                }
        }

        for (int i = 0; i < buffer.Capacity; i++)
        {
            _RemoveTest(i, false);
            _RemoveTest(i, true);
        }
    }

    [Test]
    public void RemoveVarious()
    {
        RingArray<TestItem> buffer = new RingArray<TestItem>(16, 16);
        var TestItem = new TestItem()
        {
            value = 1
        };
        
        buffer.Add(TestItem);
        buffer.RemoveAt(0);
        Assert.AreEqual(buffer.Count, 0);
        
        buffer.Add(TestItem);
        buffer.RemoveFirst(TestItem, static (in TestItem c, in TestItem x) => EqualityComparer<TestItem>.Default.Equals(x,c), true);
        Assert.AreEqual(buffer.Count, 0);
        
        buffer.Add(TestItem);
        buffer.RemoveAll(TestItem, static (in TestItem c, in TestItem x) => EqualityComparer<TestItem>.Default.Equals(x,c), true);
        Assert.AreEqual(buffer.Count, 0);
        
        buffer.Add(TestItem);
        buffer.RemoveFirstSorted(TestItem, static (in TestItem c, in TestItem x) => x.value.CompareTo(c.value), true);
        Assert.AreEqual(buffer.Count, 0);
    }
    
    
    // A Test behaves as an ordinary method
    [Test]
    public void AppendArray()
    {
        RingArray<int> buffer = new RingArray<int>(16, 2048);

        // from empty
        {
            buffer.Append(new int[2]);
            Assert.AreEqual(buffer.Head, 0);
            Assert.AreEqual(buffer.Tail, 1);
            Assert.AreEqual(buffer.Count, 2);
        }

        // wrapped
        {
            buffer.RemoveRange(0, 1);
            Assert.AreEqual(buffer.Head, 1);
            Assert.AreEqual(buffer.Tail, 1);
            Assert.AreEqual(buffer.Count, 1);

            buffer.Append(new int[buffer.Capacity-buffer.Count]);
            Assert.AreEqual(buffer.Count, buffer.Capacity);
        }

        
        // non-wrapped (fill to end)
        {
            buffer.RemoveRange(1, buffer.Count-1);
            buffer.Append(new int[buffer.Capacity - buffer.Count]);
        }
        
        // overflow capacity 
        {
            buffer.Append(new int[buffer.Capacity]);
        }
        
        
        // overflow capacity by a lot
        {
            buffer.Append(new int[buffer.Capacity*5]);
        }
    }
}
