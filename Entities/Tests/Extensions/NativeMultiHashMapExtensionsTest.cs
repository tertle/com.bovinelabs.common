// <copyright file="NativeMultiHashMapExtensionsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Entities.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Entities.Extensions;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Random = UnityEngine.Random;

    /// <summary>
    /// The NativeMultiHashMapExtensions.
    /// </summary>
    [TestFixture(typeof(int), typeof(int), typeof(Generator1), 100, 10)]
    [TestFixture(typeof(int2), typeof(int), typeof(Generator2), 100, 10)]
    [TestFixture(typeof(Entity), typeof(float4), typeof(Generator3), 100, 10)]
    [TestFixture(typeof(double2), typeof(int), typeof(Generator4), 100, 10)]
    [TestFixture(typeof(float4), typeof(Entity), typeof(Generator5), 100, 10)]
    public class NativeMultiHashMapExtensionsTest<TKey, TValue, TGenerator>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
        where TGenerator : class, IGenerateData<TKey, TValue>, new()
    {
        private readonly TGenerator _generator;
        private readonly int _keySize;
        private readonly int _valueSize;
        private int Size => _keySize * _valueSize;

        private NativeMultiHashMap<TKey, TValue> _nativeMultiHashMap;
        private Dictionary<TKey, List<TValue>> _checkingResults;
        private TKey[] _keyData;
        private TValue[] _valueData;

        public NativeMultiHashMapExtensionsTest(int keySize, int valueSize)
        {
            _generator = new TGenerator();
            _keySize = keySize;
            _valueSize = valueSize;
        }

        [SetUp]
        public void SetUp()
        {
            _nativeMultiHashMap = new NativeMultiHashMap<TKey, TValue>(Size, Allocator.Persistent);
            _checkingResults = new Dictionary<TKey, List<TValue>>();
            _keyData = new TKey[_keySize];
            _valueData = new TValue[_valueSize];

            for (var i = 0; i < _keySize; i++)
            {
                _keyData[i] = _generator.GetKey(i);
            }

            for (var i = 0; i < _valueSize; i++)
            {
                _valueData[i] = _generator.GetValue(i);
            }

            SetBaseData();
        }

        [TearDown]
        public void TearDown()
        {
            if (_nativeMultiHashMap.IsCreated)
            {
                _nativeMultiHashMap.Dispose();
            }

            _checkingResults = null;
            _keyData = null;
            _valueData = null;
        }

        #region CheckData

        [Test]
        public void CorrectData()
        {
            CheckData(Size);
        }

        [Test]
        public void CorrectDataAfterRemoveFew()
        {
            RemoveFew(out var newSize);
            CheckData(newSize);
        }

        [Test]
        public void CorrectDataAfterRemoveRandom()
        {
            RemoveRandom(out var newSize);
            CheckData(newSize);
        }

        #endregion

        #region CheckSize

        [Test]
        public void CorrectSize()
        {
            CountGetEnumerator(Size);
        }

        [Test]
        public void CorrectSizeAfterRemoveFew()
        {
            RemoveFew(out var newSize);
            CountGetEnumerator(newSize);
        }

        [Test]
        public void CorrectSizeAfterRemoveRandom()
        {
            RemoveRandom(out var newSize);
            CountGetEnumerator(newSize);
        }

        #endregion

        #region Helpers

        private void SetBaseData()
        {
            for (var i = 0; i < _keySize; i++)
            {
                for (var j = 0; j < _valueSize; j++)
                {
                    _nativeMultiHashMap.Add(_keyData[i], _valueData[j]);
                    if (_checkingResults.ContainsKey(_keyData[i]))
                    {
                        Assert.IsTrue(_checkingResults.TryGetValue(_keyData[i], out var values));
                        values.Add(_valueData[j]);
                    }
                    else
                    {
                        _checkingResults.Add(_keyData[i], new List<TValue> {_valueData[j]});
                    }
                }

                Assert.IsTrue(_checkingResults.ContainsKey(_keyData[i]));
            }

            Assert.AreEqual(Size, _nativeMultiHashMap.Length);
            Assert.AreEqual(_nativeMultiHashMap.Length, Length(_checkingResults));
        }

        private void CheckData(int size)
        {
            var result = new Dictionary<TKey, List<TValue>>();
            using (var nativeMultiHashMapEnumerator = _nativeMultiHashMap.GetEnumerator())
            {
                while (nativeMultiHashMapEnumerator.MoveNext())
                {
                    var data = nativeMultiHashMapEnumerator.Current;
                    if (result.ContainsKey(data.Key))
                    {
                        Assert.IsTrue(result.TryGetValue(data.Key, out var values));
                        values.Add(data.Value);
                    }
                    else
                    {
                        result.Add(data.Key, new List<TValue> {data.Value});
                    }

                    Assert.IsTrue(result.ContainsKey(data.Key));
                }
            }

            Assert.AreEqual(size, Length(result));
            Assert.AreEqual(
                result.Keys.Intersect(_checkingResults.Keys).Count(),
                result.Keys.Union(_checkingResults.Keys).Count()
            );

            foreach (var values in result)
            {
                Assert.IsTrue(_checkingResults.TryGetValue(values.Key, out var compareValues));
                Assert.AreEqual(
                    values.Value.Intersect(compareValues).Count(),
                    values.Value.Union(compareValues).Count()
                );
            }
        }

        private void CountGetEnumerator(int size)
        {
            var count = 0;
            using (var nativeMultiHashMapEnumerator = _nativeMultiHashMap.GetEnumerator())
            {
                while (nativeMultiHashMapEnumerator.MoveNext())
                {
                    count++;
                }
            }

            Assert.AreEqual(size, count);
        }

        private void RemoveFew(out int newSize)
        {
            Assert.IsTrue(_keySize > 1);

            _nativeMultiHashMap.Remove(_keyData[0]);
            _checkingResults.Remove(_keyData[0]);

            _nativeMultiHashMap.Remove(_keyData[_keySize - 1]);
            _checkingResults.Remove(_keyData[_keySize - 1]);

            newSize = (_keySize - 2) * _valueSize;

            Assert.AreEqual(newSize, _nativeMultiHashMap.Length);
            Assert.AreEqual(newSize, Length(_checkingResults));
        }

        private void RemoveRandom(out int newSize)
        {
            var count = 0;
            for (var i = 0; i < _keySize; i++)
            {
                if (Random.Range(0, 2) == 1)
                {
                    _nativeMultiHashMap.Remove(_keyData[i]);
                    _checkingResults.Remove(_keyData[i]);
                }
                else
                {
                    count++;
                }
            }

            newSize = count * _valueSize;

            Assert.AreEqual(newSize, _nativeMultiHashMap.Length);
            Assert.AreEqual(newSize, Length(_checkingResults));
        }

        private static int Length(Dictionary<TKey, List<TValue>> data)
        {
            var count = 0;
            using (var enumerator = data.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var values = enumerator.Current;
                    count += values.Value.Count;
                }
            }

            return count;
        }

        #endregion
    }
}