// <copyright file="NativeHashMapExtensionsTests.cs" company="BovineLabs">
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
    /// The NativeHashMapExtensions.
    /// </summary>
    [TestFixture(typeof(int), typeof(int), typeof(Generator1), 1000)]
    [TestFixture(typeof(int2), typeof(int), typeof(Generator2), 1000)]
    [TestFixture(typeof(Entity), typeof(float4), typeof(Generator3), 1000)]
    [TestFixture(typeof(double2), typeof(int), typeof(Generator4), 1000)]
    [TestFixture(typeof(float4), typeof(Entity), typeof(Generator5), 1000)]
    public class NativeHashMapExtensionsTest<TKey, TValue, TGenerator>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
        where TGenerator : class, IGenerateData<TKey, TValue>, new()
    {
        private readonly TGenerator _generator;
        private readonly int _size;

        private NativeHashMap<TKey, TValue> _nativeHashMap;
        private Dictionary<TKey, TValue> _checkingResults;
        private TKey[] _keyData;
        private TValue[] _valueData;

        public NativeHashMapExtensionsTest(int size)
        {
            _generator = new TGenerator();
            _size = size;
        }

        [SetUp]
        public void SetUp()
        {
            _nativeHashMap = new NativeHashMap<TKey, TValue>(_size, Allocator.Persistent);
            _checkingResults = new Dictionary<TKey, TValue>();
            _keyData = new TKey[_size];
            _valueData = new TValue[_size];

            for (var i = 0; i < _size; i++)
            {
                _keyData[i] = _generator.GetKey(i);
                _valueData[i] = _generator.GetValue(i);
            }

            SetBaseData();
        }

        [TearDown]
        public void TearDown()
        {
            if (_nativeHashMap.IsCreated)
            {
                _nativeHashMap.Dispose();
            }

            _checkingResults = null;
            _keyData = null;
            _valueData = null;
        }

        #region CheckData

        [Test]
        public void CorrectData()
        {
            CheckData(_size);
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
            CountGetEnumerator(_size);
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
            for (var i = 0; i < _size; i++)
            {
                Assert.IsFalse(_nativeHashMap.TryGetValue(_keyData[i], out _));
                Assert.IsTrue(_nativeHashMap.TryAdd(_keyData[i], _valueData[i]));
                Assert.IsTrue(_nativeHashMap.TryGetValue(_keyData[i], out _));

                Assert.IsFalse(_checkingResults.ContainsKey(_keyData[i]));
                _checkingResults.Add(_keyData[i], _valueData[i]);
            }

            Assert.AreEqual(_size, _nativeHashMap.Length);
            Assert.AreEqual(_nativeHashMap.Length, _checkingResults.Count);
        }

        private void CheckData(int size)
        {
            var result = new Dictionary<TKey, TValue>();
            using (var nativeHashMapEnumerator = _nativeHashMap.GetEnumerator())
            {
                while (nativeHashMapEnumerator.MoveNext())
                {
                    var data = nativeHashMapEnumerator.Current;
                    Assert.IsFalse(result.ContainsKey(data.Key));
                    result.Add(data.Key, data.Value);
                }
            }

            Assert.AreEqual(size, result.Count);
            Assert.AreEqual(result.Intersect(_checkingResults).Count(), result.Union(_checkingResults).Count());
        }

        private void CountGetEnumerator(int size)
        {
            var count = 0;
            using (var nativeHashMapEnumerator = _nativeHashMap.GetEnumerator())
            {
                while (nativeHashMapEnumerator.MoveNext())
                {
                    count++;
                }
            }

            Assert.AreEqual(size, count);
        }

        private void RemoveFew(out int newSize)
        {
            Assert.IsTrue(_size > 1);

            _nativeHashMap.Remove(_keyData[0]);
            _checkingResults.Remove(_keyData[0]);

            _nativeHashMap.Remove(_keyData[_size - 1]);
            _checkingResults.Remove(_keyData[_size - 1]);

            newSize = _size - 2;

            Assert.AreEqual(newSize, _nativeHashMap.Length);
            Assert.AreEqual(newSize, _checkingResults.Count);
        }

        private void RemoveRandom(out int newSize)
        {
            newSize = 0;
            for (var i = 0; i < _size; i++)
            {
                if (Random.Range(0, 2) == 1)
                {
                    _nativeHashMap.Remove(_keyData[i]);
                    _checkingResults.Remove(_keyData[i]);
                }
                else
                {
                    newSize++;
                }
            }

            Assert.AreEqual(newSize, _nativeHashMap.Length);
            Assert.AreEqual(newSize, _checkingResults.Count);
        }

        #endregion
    }
}