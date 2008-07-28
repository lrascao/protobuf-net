﻿#if REMOTING
using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using ProtoBuf;
using NUnit.Framework;
using System.IO;

namespace Examples.Remoting
{
    [Serializable]
    public sealed class RegularFragment
    {
        public int Foo { get; set; }
        public float Bar { get; set; }
    }
    [Serializable, ProtoContract]
    public sealed class ProtoFragment : ISerializable
    {
        [ProtoMember(1, DataFormat=DataFormat.TwosComplement)]
        public int Foo { get; set; }
        [ProtoMember(2)]
        public float Bar { get; set; }

        public ProtoFragment() { }
        private ProtoFragment(SerializationInfo info, StreamingContext context)
        {
            Serializer.Merge(info, this);
        }
        void  ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Serializer.Serialize(info, this);
        }
}

    public sealed class Server : MarshalByRefObject
    {
        public RegularFragment SomeMethod1(RegularFragment value)
        {
            return new RegularFragment {
                Foo = value.Foo * 2,
                Bar = value.Bar * 2
            };
        }
        public ProtoFragment SomeMethod2(ProtoFragment value)
        {
            return new ProtoFragment
            {
                Foo = value.Foo * 2,
                Bar = value.Bar * 2
            };
        }
    }

    [TestFixture]
    public class RemotingDemo
    {
        //[Test]

        //damn can't get this one working at the moment...
        public void RunRemotingDemo()
        {
            AppDomain app = AppDomain.CreateDomain("Isolated");
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,

                GetType().Assembly.ManifestModule.Name);
            byte[] raw = File.ReadAllBytes(path);
            app.Load(raw);
            try
            {
                // create a server and two identical messages
                Server local = new Server(),
                    remote = (Server)app.CreateInstanceAndUnwrap(typeof(Server).Assembly.FullName, typeof(Server).FullName);
                RegularFragment frag1 = new RegularFragment { Foo = 27, Bar = 123.45F };
                ProtoFragment frag2 = new ProtoFragment { Foo = frag1.Foo, Bar = frag1.Bar };
                // verify basic transport
                RegularFragment localFrag1 = local.SomeMethod1(frag1),
                    remoteFrag1 = remote.SomeMethod1(frag1);
                ProtoFragment localFrag2 = local.SomeMethod2(frag2),
                    remoteFrag2 = remote.SomeMethod2(frag2);

                Assert.AreEqual(localFrag1.Foo, remoteFrag1.Foo);
                Assert.AreEqual(localFrag1.Bar, remoteFrag1.Bar);
                Assert.AreEqual(localFrag2.Foo, remoteFrag2.Foo);
                Assert.AreEqual(localFrag2.Bar, remoteFrag2.Bar);
                Assert.AreEqual(localFrag1.Foo, localFrag2.Foo);
                Assert.AreEqual(localFrag1.Bar, localFrag2.Bar);

            }
            finally
            {                
                AppDomain.Unload(app);
            }
        }

    }
}
#endif