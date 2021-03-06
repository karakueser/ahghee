﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Ahghee;
using Ahghee.Grpc;
using benchmark;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using BenchmarkDotNet.Running;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.FSharp.Core;
using RocksDbSharp;

namespace benchmark
{
    [MinColumn, MaxColumn]
    public class RocksDbSinglePut:IDisposable
    {
        private readonly RocksDb db;
        private readonly NodeIdIndex nodeIndex;
        private int ctr;
        private readonly MemoryPointer mp;
        private readonly Pointers rp;
        private readonly ConcurrentDictionary<int,Pointers> cd= new ConcurrentDictionary<int,Pointers>();
        private int idHash;
        private NodeID Nodeid;
        public RocksDbSinglePut()
        {
            var temp = Path.GetTempPath();
            var options = (new DbOptions()).SetCreateIfMissing(true).EnableStatistics();
            db = RocksDb.Open(options, Environment.ExpandEnvironmentVariables(Path.Combine(temp, Path.GetRandomFileName())));

            nodeIndex = new NodeIdIndex(Environment.ExpandEnvironmentVariables(Path.Combine(temp, Path.GetRandomFileName())));
            
            Nodeid = new NodeID();
            Nodeid.Pointer = Utils.NullMemoryPointer();
            Nodeid.Graph = "graph";
            Nodeid.Nodeid = "1";
            
            rp = new Pointers();
            mp = Utils.NullMemoryPointer();
            mp.Offset = 100UL;
            mp.Length = 200UL;
            rp.Pointers_.Add(mp);
           

        }
        
        [Benchmark(Baseline = true)]
        public void PutRocksDbStrings()
        {
            db.Put(ctr.ToString(), "value");
            Interlocked.Increment(ref ctr);
        }
        
        [Benchmark]
        public void PutNodeIdIndex()
        {
            Nodeid.Nodeid = Interlocked.Increment(ref ctr).ToString();
            var idHash = Utils.GetNodeIdHash(Nodeid);
            var value = nodeIndex.AddOrUpdateCS(idHash, () => rp, (id, rp) => rp);   
        }
        [Benchmark]
        public void PutConcurrentDictionary()
        {
            Nodeid.Nodeid = Interlocked.Increment(ref ctr).ToString();
            var idHash = Utils.GetNodeIdHash(Nodeid);
            var value = cd.AddOrUpdate(idHash, rp, (id, rp) => rp);   
        }

        private void ReleaseUnmanagedResources()
        {
            db.Dispose();
            (nodeIndex as IDisposable).Dispose();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~RocksDbSinglePut()
        {
            ReleaseUnmanagedResources();
        }
    }
    
    [MinColumn, MaxColumn]
    public class CreatingNodeEmpty
    {
        [Benchmark(Baseline = true)]
        public Node MkNodeEmpty() => Ahghee.Utils.Node(new AddressBlock(), Array.Empty<KeyValue>());

    }

    [MinColumn, MaxColumn]
    public class CreatingKeyValue
    {
        private static string _graph = "graph1";
        private static string _id = "12345";
        private static string _key1 = "name";
        private static string _value1 = "Austin";
        private byte[] _value1Bytes;
        private byte[] _key1Bytes;
        private ByteString _key1ProtoBytes;
        private ByteString _value1ProtoBytes;

        public CreatingKeyValue()
        {
            _key1Bytes = Encoding.UTF8.GetBytes(_key1);
            _value1Bytes = Encoding.UTF8.GetBytes(_value1);
            _key1ProtoBytes = Google.Protobuf.ByteString.CopyFrom(_key1Bytes);
            _value1ProtoBytes = Google.Protobuf.ByteString.CopyFrom(_value1Bytes);
        }
        
        [Benchmark(Baseline = true)]
        public KeyValue MkKvListString() {
            var kv = Ahghee.Utils.PropString(_key1, _value1);
            return kv;
        }
        
        [Benchmark]
        public KeyValue MkKvArrayString()
        {
            var kv = Ahghee.Utils.PropString(_key1, _value1);
            return kv;
        }
        
        [Benchmark]
        public KeyValue MkKvManual1()
        {
            var kv = new KeyValue();
            kv.Key = Ahghee.Utils.TMDAuto(Ahghee.Utils.DBBString(_key1));
            kv.Value = Ahghee.Utils.TMDAuto(Ahghee.Utils.DBBString(_value1));
            return kv;
        }
        
        [Benchmark]
        public KeyValue MkKvManual2()
        {
            var kv = new KeyValue();
            kv.Key = Ahghee.Utils.TMDAuto(Ahghee.Utils.DBB(Ahghee.Utils.MetaBytes(Ahghee.Utils.metaPlainTextUtf8,_key1Bytes)));
            kv.Value = Ahghee.Utils.TMDAuto(Ahghee.Utils.DBB(Ahghee.Utils.MetaBytes(Ahghee.Utils.metaPlainTextUtf8,_value1Bytes)));
            return kv;
        }
        
        [Benchmark]
        public KeyValue MkKvManual3()
        {
            var kv = new KeyValue();
            kv.Key = Ahghee.Utils.TMDAuto(Ahghee.Utils.DBB(Ahghee.Utils.MetaBytesNoCopy(Ahghee.Utils.metaPlainTextUtf8,_key1ProtoBytes)));
            kv.Value = Ahghee.Utils.TMDAuto(Ahghee.Utils.DBB(Ahghee.Utils.MetaBytesNoCopy(Ahghee.Utils.metaPlainTextUtf8,_value1ProtoBytes)));
            return kv;
        }
    }

    [MinColumn, MaxColumn]
    public class CreatingTypes
    {
        [Benchmark]
        public DataBlock MkBinaryBlockString() => Ahghee.Utils.DBBString(_key1);
        
        [Benchmark]
        public BinaryBlock MkBinaryBlockEmpty() => new BinaryBlock();

        [Benchmark]
        public AddressBlock MkAddressBlockEmpty() => new AddressBlock();

        [Benchmark]
        public DataBlock MkDataBlockEmpty() => new DataBlock();

        [Benchmark]
        public KeyValue MkKeyValueEmpty() => new KeyValue();

        [Benchmark]
        public Node MkNodeEmpty() => Ahghee.Utils.Node(new AddressBlock(), Array.Empty<KeyValue>());

        
        private static string _graph = "graph1";
        private static string _id = "12345";
        private static string _key1 = "name";
        private static string _value1 = "Austin";
        
        [Benchmark]
        public AddressBlock MkIdSimple() {
            var id = Ahghee.Utils.Id(_graph, _id, Ahghee.Utils.NullMemoryPointer());
            return id;
        }
        
        [Benchmark]
        public KeyValue MkKvSimple() {
            var kv = Ahghee.Utils.PropString(_key1, _value1);
            return kv;
        }
        
        [Benchmark]
        public Node MkNodeSimple() {
            var id = Ahghee.Utils.Id(_graph, _id, Ahghee.Utils.NullMemoryPointer());
            var kv = Ahghee.Utils.PropString(_key1, _value1);
            return Ahghee.Utils.Node(id, new List<KeyValue>{kv});
        }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            //var summary0 = BenchmarkRunner.Run<CreatingTypes>();
            //var summary1 = BenchmarkRunner.Run<CreatingKeyValue>();
            //var summary2 = BenchmarkRunner.Run<CreatingNodeEmpty>();
            var summary3 = BenchmarkRunner.Run<RocksDbSinglePut>();
        }
    }
}