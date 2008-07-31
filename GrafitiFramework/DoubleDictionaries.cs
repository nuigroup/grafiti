/*
	Grafiti library

    Copyright 2008  Alessandro De Nardi <alessandro.denardi@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License as
    published by the Free Software Foundation; either version 3 of 
    the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.

*/

using System;
using System.Collections.Generic;

namespace Grafiti
{
    public class DoubleDictionary<TKey1, TKey2, TValue>
    {
        private readonly int m_initialCapacity;
        private readonly Dictionary<TKey1, Dictionary<TKey2, TValue>> m_table1;

        public DoubleDictionary() : this(100) { }

        public DoubleDictionary(int capacity)
        {
            m_initialCapacity = capacity;
            m_table1 = new Dictionary<TKey1, Dictionary<TKey2, TValue>>(m_initialCapacity);
        }

        public bool Remove(TKey1 key1, TKey2 key2)
        {
            bool elementHasBeenRemoved = false;
            if (m_table1.ContainsKey(key1))
            {
                elementHasBeenRemoved = m_table1[key1].Remove(key2);
                if (m_table1[key1].Count == 0)
                    m_table1.Remove(key1);
            }
            return elementHasBeenRemoved;
        }

        public TValue this[TKey1 key1, TKey2 key2]
        {
            get
            {
                return m_table1[key1][key2];
            }
            set
            {
                Dictionary<TKey2, TValue> table2;
                if (!m_table1.ContainsKey(key1))
                {
                    table2 = new Dictionary<TKey2, TValue>();
                    m_table1[key1] = table2;
                    table2[key2] = value;
                }
                else
                    m_table1[key1][key2] = value;
            }
        }

        public bool ContainsKeys(TKey1 key1, TKey2 key2)
        {
            if (!m_table1.ContainsKey(key1))
                return false;
            return m_table1[key1].ContainsKey(key2);
        }

        public bool TryGetValue(TKey1 key1, TKey2 key2, out TValue value)
        {
            if (ContainsKeys(key1, key2))
            {
                value = this[key1, key2];
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }

        }
    }

    public class SimmetricDoubleDictionary<TKey, TValue> where TKey : IComparable
    {
        private readonly int m_initialCapacity;
        private readonly Dictionary<TKey, Dictionary<TKey, TValue>> m_table1;

        public SimmetricDoubleDictionary() : this(100) { }

        public SimmetricDoubleDictionary(int capacity)
        {
            m_initialCapacity = capacity;
            m_table1 = new Dictionary<TKey, Dictionary<TKey, TValue>>(m_initialCapacity);
        }

        public bool Remove(TKey key)
        {
            bool elementHasBeenRemoved = false;
            foreach (TKey key1 in m_table1.Keys)
            {
                if (key1.CompareTo(key) <= 0)
                    if (m_table1[key1].Remove(key))
                        elementHasBeenRemoved = true;
            }
            if (m_table1.Remove(key))
                elementHasBeenRemoved = true;
            return elementHasBeenRemoved;
        }

        public TValue this[TKey key1, TKey key2]
        {
            get
            {
                if (key1.CompareTo(key2) > 0)
                {
                    TKey temp = key1;
                    key1 = key2;
                    key2 = temp;
                }

                return m_table1[key1][key2];
            }
            set
            {
                if (key1.CompareTo(key2) > 0)
                {
                    TKey temp = key1;
                    key1 = key2;
                    key2 = temp;
                }

                Dictionary<TKey, TValue> table2;
                if (!m_table1.ContainsKey(key1))
                {
                    table2 = new Dictionary<TKey, TValue>();
                    m_table1[key1] = table2;
                    table2[key2] = value;
                }
                else
                    m_table1[key1][key2] = value;
            }
        }

        // TODO: ContainsKeys and TryGetValue
    }
}
