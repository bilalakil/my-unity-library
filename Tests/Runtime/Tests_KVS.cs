using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyLibrary
{
    /**
     * WARNING: Running these tests will nuke the default KVS data!
     */
    public class Tests_KVS
    {
        [SetUp]
        [TearDown]
        public void ResetKVS()
        {
            KVS.Deinit();
            DestroyKVSOnDisk();
        }

#region PlayerPrefs interface
        [Test]
        public void DeleteAll_Works()
        {
            GivenKVSOnDisk(new KVS.Data { floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="test", val=-9.9f } } });
            WhenKVSDeleteAll();
            ThenKVSOnDiskMatches(new KVS.Data());
            
            WhenKVSSetFloat("test", 3.6f);
            WhenKVSSaved();
            ThenKVSOnDiskMatches(new KVS.Data { floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="test", val=3.6f } } });

            WhenKVSDeleteAll();
            ThenKVSOnDiskMatches(new KVS.Data());
        }

        [Test]
        public void DeleteKey_WorksFromFile()
        {
            GivenKVSOnDisk(new KVS.Data {
                floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="testfloat", val=0.4f } },
                ints=new List<KVS.KVInt> { new KVS.KVInt { key="testint", val=4 } },
                strs=new List<KVS.KVString> { new KVS.KVString { key="teststr", val="free" } },
            });
            WhenKVSDeleteKey("testfloat");
            WhenKVSDeleteKey("testint");
            WhenKVSDeleteKey("teststr");
            ThenKVSGetFloatEquals("testfloat", 0.0f);
            ThenKVSGetIntEquals("testint", 0);
            ThenKVSGetStringEquals("teststring", "");

            WhenKVSSaved();
            ThenKVSOnDiskMatches(new KVS.Data());
        }

        [Test]
        public void DeleteKey_WorksOnUnsavedValues()
        {
            GivenKVSOnDisk(new KVS.Data {
                floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="testfloat", val=0.4f } },
                ints=new List<KVS.KVInt> { new KVS.KVInt { key="testint", val=4 } },
                strs=new List<KVS.KVString> { new KVS.KVString { key="teststr", val="free" } },
            });
            WhenKVSSetFloat("testfloat2", -1.2f);
            WhenKVSSetInt("testint2", -14);
            WhenKVSSetString("teststr2", "hale");
            WhenKVSDeleteKey("testfloat2");
            WhenKVSDeleteKey("testint2");
            WhenKVSDeleteKey("teststr2");
            ThenKVSGetFloatEquals("testfloat2", 0.0f);
            ThenKVSGetIntEquals("testint2", 0);
            ThenKVSGetStringEquals("teststring2", "");

            WhenKVSSaved();
            ThenKVSOnDiskMatches(new KVS.Data {
                floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="testfloat", val=0.4f } },
                ints=new List<KVS.KVInt> { new KVS.KVInt { key="testint", val=4 } },
                strs=new List<KVS.KVString> { new KVS.KVString { key="teststr", val="free" } },
            });
        }

        [Test]
        public void DeleteKey_DoesNotThrowIfDeletingNothing()
        {
            WhenKVSDeleteKey("i");
            WhenKVSDeleteKey("dont");
            WhenKVSDeleteKey("throw");
            // No throw
        }

        [Test]
        public void GetFloat_WorksFromFile()
        {
            GivenKVSOnDisk(new KVS.Data { floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="test", val=-9.9f } } });
            ThenKVSGetFloatEquals("test", -9.9f);
        }
        
        [Test]
        public void GetFloat_DefaultValueWorks()
        {
            // GIVEN no KVS data
            ThenKVSGetFloatEquals("test", 4.0f, 4.0f);
            ThenKVSGetFloatEquals("test", -3.3f, -3.3f);

            WhenKVSSetFloat("test", 3.6f);
            ThenKVSGetFloatEquals("test", 3.6f, 9.1f);
        }

        [Test]
        public void GetFloat_DefaultReturnedForOtherTypes()
        {
            GivenKVSOnDisk(new KVS.Data {
                ints=new List<KVS.KVInt> { new KVS.KVInt { key="testint", val=4 } },
                strs=new List<KVS.KVString> { new KVS.KVString { key="teststr", val="teststr" } },
            });
            ThenKVSGetFloatEquals("testint", 0.0f);
            ThenKVSGetFloatEquals("testint", 3.23f, 3.23f);
            ThenKVSGetFloatEquals("teststr", 0);
            ThenKVSGetFloatEquals("teststr", -1.23f, -1.23f);

            WhenKVSSetInt("testint", -10);
            WhenKVSSetString("teststr", "anotherteststr");
            ThenKVSGetFloatEquals("testint", 0.0f);
            ThenKVSGetFloatEquals("testint", 2.0f, 2.0f);
            ThenKVSGetFloatEquals("teststr", 0.0f);
            ThenKVSGetFloatEquals("teststr", -11f, -11f);
        }

        [Test]
        public void GetInt_WorksFromFile()
        {
            GivenKVSOnDisk(new KVS.Data { ints=new List<KVS.KVInt> { new KVS.KVInt { key="test", val=-9 } } });
            ThenKVSGetIntEquals("test", -9);
        }
        
        [Test]
        public void GetInt_DefaultValueWorks()
        {
            // GIVEN no KVS data
            ThenKVSGetIntEquals("test", 4, 4);
            ThenKVSGetIntEquals("test", -3, -3);

            WhenKVSSetInt("test", 6);
            ThenKVSGetIntEquals("test", 6, 9);
        }

        [Test]
        public void GetInt_DefaultReturnedForOtherTypes()
        {
            GivenKVSOnDisk(new KVS.Data {
                floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="testfloat", val=0.4f } },
                strs=new List<KVS.KVString> { new KVS.KVString { key="teststr", val="teststr" } },
            });
            ThenKVSGetIntEquals("testfloat", 0);
            ThenKVSGetIntEquals("testfloat", 4, 4);
            ThenKVSGetIntEquals("teststr", 0);
            ThenKVSGetIntEquals("teststr", -3, -3);

            WhenKVSSetFloat("testfloat", -0.1f);
            WhenKVSSetString("teststr", "anotherteststr");
            ThenKVSGetIntEquals("testfloat", 0);
            ThenKVSGetIntEquals("testfloat", 2, 2);
            ThenKVSGetIntEquals("teststr", 0);
            ThenKVSGetIntEquals("teststr", -1, -1);
        }

        [Test]
        public void GetString_WorksFromFile()
        {
            GivenKVSOnDisk(new KVS.Data { strs=new List<KVS.KVString> { new KVS.KVString { key="test", val="asd" } } });
            ThenKVSGetStringEquals("test", "asd");
        }
        
        [Test]
        public void GetString_DefaultValueWorks()
        {
            // GIVEN no KVS data
            ThenKVSGetStringEquals("test", "ddd", "ddd");
            ThenKVSGetStringEquals("test", "moo", "moo");

            WhenKVSSetString("test", "great");
            ThenKVSGetStringEquals("test", "great", "terrible");
        }

        [Test]
        public void GetString_DefaultReturnedForOtherTypes()
        {
            GivenKVSOnDisk(new KVS.Data {
                floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="testfloat", val=0.4f } },
                ints=new List<KVS.KVInt> { new KVS.KVInt { key="testint", val=4 } },
            });
            ThenKVSGetStringEquals("testfloat", "");
            ThenKVSGetStringEquals("testfloat", "moo", "moo");
            ThenKVSGetStringEquals("testint", "");
            ThenKVSGetStringEquals("testint", "hey", "hey");

            WhenKVSSetFloat("testfloat", -0.1f);
            WhenKVSSetInt("testint", 12);
            ThenKVSGetStringEquals("testfloat", "");
            ThenKVSGetStringEquals("testfloat", "noice", "noice");
            ThenKVSGetStringEquals("testint", "");
            ThenKVSGetStringEquals("testint", "gotya", "gotya");
        }

        [Test]
        public void HasKey_Works()
        {
            GivenKVSOnDisk(new KVS.Data {
                floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="testfloat", val=0.4f } },
                ints=new List<KVS.KVInt> { new KVS.KVInt { key="testint", val=4 } },
                strs=new List<KVS.KVString> { new KVS.KVString { key="teststr", val="free" } },
            });
            ThenKVSHasKey("testfloat", true);
            ThenKVSHasKey("testfloat2", false);
            ThenKVSHasKey("testint", true);
            ThenKVSHasKey("testint2", false);
            ThenKVSHasKey("teststr", true);
            ThenKVSHasKey("teststr2", false);

            WhenKVSDeleteKey("testfloat");
            WhenKVSDeleteKey("testint");
            WhenKVSDeleteKey("teststr");
            WhenKVSSetFloat("testfloat2", -11.4f);
            WhenKVSSetInt("testint2", 58);
            WhenKVSSetString("teststr2", "hei");
            ThenKVSHasKey("testfloat", false);
            ThenKVSHasKey("testfloat2", true);
            ThenKVSHasKey("testint", false);
            ThenKVSHasKey("testint2", true);
            ThenKVSHasKey("teststr", false);
            ThenKVSHasKey("teststr2", true);
        }

        [Test]
        public void Save_Works()
        {
            WhenKVSSetFloat("test", 5.2f);
            WhenKVSSetFloat("test2", -13.1f);
            WhenKVSDeleteKey("test2");
            ThenKVSOnDiskMatches(new KVS.Data());

            WhenKVSSaved();
            ThenKVSOnDiskMatches(new KVS.Data { floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="test", val=5.2f } } });

            WhenKVSDeleteKey("test");
            ThenKVSOnDiskMatches(new KVS.Data { floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="test", val=5.2f } } });

            WhenKVSSaved();
            ThenKVSOnDiskMatches(new KVS.Data());
        }
        
        [Test]
        public void SetFloat_Works()
        {
            WhenKVSSetFloat("test", 3.6f);
            ThenKVSGetFloatEquals("test", 3.6f);

            WhenKVSSetFloat("test", -1.2f);
            ThenKVSGetFloatEquals("test", -1.2f);

            WhenKVSSaved();
            ThenKVSOnDiskMatches(new KVS.Data { floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="test", val=-1.2f } } });
        }
        
        [Test]
        public void SetInt_Works()
        {
            WhenKVSSetInt("test", 2);
            ThenKVSGetIntEquals("test", 2);

            WhenKVSSetInt("test", -1);
            ThenKVSGetIntEquals("test", -1);

            WhenKVSSaved();
            ThenKVSOnDiskMatches(new KVS.Data { ints=new List<KVS.KVInt> { new KVS.KVInt { key="test", val=-1 } } });
        }
        
        [Test]
        public void SetString_Works()
        {
            WhenKVSSetString("test", "grok");
            ThenKVSGetStringEquals("test", "grok");

            WhenKVSSetString("test", "frog");
            ThenKVSGetStringEquals("test", "frog");

            WhenKVSSaved();
            ThenKVSOnDiskMatches(new KVS.Data { strs=new List<KVS.KVString> { new KVS.KVString { key="test", val="frog" } } });
        }

        [Test]
        public void Mixed_Positive()
        {
            GivenKVSOnDisk(new KVS.Data {
                floats=new List<KVS.KVFloat> { 
                    new KVS.KVFloat { key="float1", val=float.MaxValue },
                    new KVS.KVFloat { key="float2", val=float.MinValue },
                },
                ints=new List<KVS.KVInt> {
                    new KVS.KVInt { key="int1", val=400 },
                    new KVS.KVInt { key="int2", val=int.MinValue },
                    new KVS.KVInt { key="int3", val=int.MaxValue },
                },
                strs=new List<KVS.KVString> {
                    new KVS.KVString { key="string1", val="one" },
                    new KVS.KVString { key="string2", val="TWO" },
                    new KVS.KVString { key="string3", val="T#$!@" },
                    new KVS.KVString { key="string4", val="fou4" },
                },
            });

            ThenKVSGetFloatEquals("float1", float.MaxValue, -1f);
            ThenKVSGetFloatEquals("float2", float.MinValue, -2f);
            ThenKVSGetFloatEquals("float3", 0.0f);
            ThenKVSGetFloatEquals("float3", 2f, 2f);
            ThenKVSHasKey("float2", true);
            ThenKVSHasKey("float3", false);
            ThenKVSGetIntEquals("int2", int.MinValue, -1);
            ThenKVSGetIntEquals("int3", int.MaxValue, -2);
            ThenKVSGetIntEquals("int4", 0);
            ThenKVSGetIntEquals("int4", -3, -3);
            ThenKVSHasKey("int3", true);
            ThenKVSHasKey("int4", false);
            ThenKVSGetStringEquals("string3", "T#$!@", "mm");
            ThenKVSGetStringEquals("string4", "fou4", "`$a");
            ThenKVSGetStringEquals("string5", "");
            ThenKVSGetStringEquals("string5", "ferniha", "ferniha");
            ThenKVSHasKey("string4", true);
            ThenKVSHasKey("string5", false);

            WhenKVSDeleteKey("float2");
            WhenKVSDeleteKey("int1");
            WhenKVSDeleteKey("int3");
            WhenKVSDeleteKey("string1");
            WhenKVSDeleteKey("string2");
            WhenKVSDeleteKey("string4");
            WhenKVSSetFloat("float1", -1f);
            WhenKVSSetFloat("float3", -40f);
            WhenKVSSetInt("int2", -12);
            WhenKVSSetInt("int4", -22);
            WhenKVSSetString("string1", "one is back");
            WhenKVSSetString("string3", "threeeee");
            WhenKVSSetString("string5", "+");

            ThenKVSGetFloatEquals("float1", -1f, 0.01f);
            ThenKVSGetFloatEquals("float3", -40f, 0.1f);
            ThenKVSHasKey("float2", false);
            ThenKVSHasKey("float3", true);
            ThenKVSGetIntEquals("int2", -12, -21);
            ThenKVSGetIntEquals("int4", -22, 33);
            ThenKVSHasKey("int3", false);
            ThenKVSHasKey("int2", true);
            ThenKVSHasKey("int4", true);
            ThenKVSGetStringEquals("string3", "threeeee", "3");
            ThenKVSGetStringEquals("string5", "+", "default");
            ThenKVSHasKey("string2", false);
            ThenKVSHasKey("string5", true);
            ThenKVSHasKey("string3", true);

            WhenKVSSaved();
            ThenKVSOnDiskMatches(new KVS.Data {
                floats=new List<KVS.KVFloat> { 
                    new KVS.KVFloat { key="float1", val=-1f },
                    new KVS.KVFloat { key="float3", val=-40f },
                },
                ints=new List<KVS.KVInt> {
                    new KVS.KVInt { key="int2", val=-12 },
                    new KVS.KVInt { key="int4", val=-22 },
                },
                strs=new List<KVS.KVString> {
                    new KVS.KVString { key="string3", val="threeeee" },
                    new KVS.KVString { key="string1", val="one is back" },
                    new KVS.KVString { key="string5", val="+" },
                },
            });
        }
#endregion

        [Test]
        public void Save_CalledOnTeardown()
        {
            WhenKVSSetFloat("test", 7.6f);
            ThenKVSOnDiskMatches(new KVS.Data());

            WhenKVSDeinitialised();
            ThenKVSOnDiskMatches(new KVS.Data { floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="test", val=7.6f } } });
        }

        [Test]
        public void InitialLoadDeletesMalformedDiskData()
        {
            GivenKVSOnDisk("this is an invalid kvs data file", true);
            WhenKVSInitialised();
            ThenKVSIsNotOnDisk();
        }
        
        void DestroyKVSOnDisk() =>
            File.Delete(KVS.FilePath);

        void GivenKVSOnDisk(KVS.Data data) =>
            GivenKVSOnDisk(JsonUtility.ToJson(data));
        
        void GivenKVSOnDisk(string data, bool isInvalid=false)
        {
            if (isInvalid)
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("^Failed to convert saved data from JSON, nuking!")
                );
            File.WriteAllText(KVS.FilePath, data);
        }
        
        void WhenKVSInitialised() =>
            KVS.GetInt("meh"); // Do anything to trigger initialisation
        
        void WhenKVSDeinitialised() =>
            KVS.Deinit();
        
        void WhenKVSDeleteAll() =>
            KVS.DeleteAll();
        
        void WhenKVSDeleteKey(string key) =>
            KVS.DeleteKey(key);
        
        void WhenKVSSaved() =>
            KVS.Save();
        
        void WhenKVSSetFloat(string key, float val) =>
            KVS.SetFloat(key, val);
        
        void WhenKVSSetInt(string key, int val) =>
            KVS.SetInt(key, val);
        
        void WhenKVSSetString(string key, string val) =>
            KVS.SetString(key, val);
        
        void ThenKVSGetFloatEquals(string key, float expected, float defaultValue=0.0f) =>
            Assert.AreEqual(expected, KVS.GetFloat(key, defaultValue));
        
        void ThenKVSGetIntEquals(string key, int expected, int defaultValue=0) =>
            Assert.AreEqual(expected, KVS.GetInt(key, defaultValue));
        
        void ThenKVSGetStringEquals(string key, string expected, string defaultValue="") =>
            Assert.AreEqual(expected, KVS.GetString(key, defaultValue));
        
        void ThenKVSHasKey(string key, bool expected) =>
            Assert.AreEqual(expected, KVS.HasKey(key));
        
        void ThenKVSOnDiskMatches(KVS.Data expected)
        {
            var json = File.Exists(KVS.FilePath)
                ? File.ReadAllText(KVS.FilePath)
                : "{}";
            var actual = JsonUtility.FromJson<KVS.Data>(json);

            Assert.AreEqual(expected.floats.Count, actual.floats.Count);
            for (var i = 0; i != actual.floats.Count; ++i)
            {
                Assert.AreEqual(expected.floats[i].key, actual.floats[i].key);
                Assert.IsTrue(Mathf.Approximately(
                    expected.floats[i].val,
                    actual.floats[i].val
                ));
            }
        }

        void ThenKVSIsNotOnDisk() =>
            Assert.IsFalse(File.Exists(KVS.FilePath));
    }
}
