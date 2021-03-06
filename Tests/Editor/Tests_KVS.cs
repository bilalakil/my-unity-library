using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyLibrary
{
    /**
     * WARNING: Running these tests will nuke the default KVS data!
     */
    public class Tests_KVS : UnityEditorBDD
    {
        const string ASSET_DIR = UnityEditorBDD.TEST_ASSET_DIR + "/KVS";

        static string[] _configsToCleanUp = new string[]
        {
            ASSET_DIR + "/TestConfigStandard",
            ASSET_DIR + "/TestConfigDifferentFilename"
        };

        [SetUp]
        public void ResetAndInitKVS()
        {
            ResetKVS();

            KVS.Init();
        }

        [TearDown]
        public void ResetKVS()
        {
            KVS.Deinit();
            DestroyKVSOnDisk(_configsToCleanUp);
        }

#region PlayerPrefs interface
        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void DeleteAll()
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
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void DeleteKey_FromFile()
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
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void DeleteKey_UnsavedValues()
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
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void DeleteKey_DoesNotThrowIfDeletingNothing()
        {
            WhenKVSDeleteKey("i");
            WhenKVSDeleteKey("dont");
            WhenKVSDeleteKey("throw");
            // No throw
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void GetFloat_FromFile()
        {
            GivenKVSOnDisk(new KVS.Data { floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="test", val=-9.9f } } });
            ThenKVSGetFloatEquals("test", -9.9f);
        }
        
        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void GetFloat_DefaultValue()
        {
            // GIVEN no KVS data
            ThenKVSGetFloatEquals("test", 4.0f, 4.0f);
            ThenKVSGetFloatEquals("test", -3.3f, -3.3f);

            WhenKVSSetFloat("test", 3.6f);
            ThenKVSGetFloatEquals("test", 3.6f, 9.1f);
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
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
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void GetInt_FromFile()
        {
            GivenKVSOnDisk(new KVS.Data { ints=new List<KVS.KVInt> { new KVS.KVInt { key="test", val=-9 } } });
            ThenKVSGetIntEquals("test", -9);
        }
        
        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void GetInt_DefaultValue()
        {
            // GIVEN no KVS data
            ThenKVSGetIntEquals("test", 4, 4);
            ThenKVSGetIntEquals("test", -3, -3);

            WhenKVSSetInt("test", 6);
            ThenKVSGetIntEquals("test", 6, 9);
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
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
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void GetString_FromFile()
        {
            GivenKVSOnDisk(new KVS.Data { strs=new List<KVS.KVString> { new KVS.KVString { key="test", val="asd" } } });
            ThenKVSGetStringEquals("test", "asd");
        }
        
        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void GetString_DefaultValue()
        {
            // GIVEN no KVS data
            ThenKVSGetStringEquals("test", "ddd", "ddd");
            ThenKVSGetStringEquals("test", "moo", "moo");

            WhenKVSSetString("test", "great");
            ThenKVSGetStringEquals("test", "great", "terrible");
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
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
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void HasKey()
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
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void Save()
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
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void SetFloat()
        {
            WhenKVSSetFloat("test", 3.6f);
            ThenKVSGetFloatEquals("test", 3.6f);

            WhenKVSSetFloat("test", -1.2f);
            ThenKVSGetFloatEquals("test", -1.2f);

            WhenKVSSaved();
            ThenKVSOnDiskMatches(new KVS.Data { floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="test", val=-1.2f } } });
        }
        
        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void SetInt()
        {
            WhenKVSSetInt("test", 2);
            ThenKVSGetIntEquals("test", 2);

            WhenKVSSetInt("test", -1);
            ThenKVSGetIntEquals("test", -1);

            WhenKVSSaved();
            ThenKVSOnDiskMatches(new KVS.Data { ints=new List<KVS.KVInt> { new KVS.KVInt { key="test", val=-1 } } });
        }
        
        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void SetString()
        {
            WhenKVSSetString("test", "grok");
            ThenKVSGetStringEquals("test", "grok");

            WhenKVSSetString("test", "frog");
            ThenKVSGetStringEquals("test", "frog");

            WhenKVSSaved();
            ThenKVSOnDiskMatches(new KVS.Data { strs=new List<KVS.KVString> { new KVS.KVString { key="test", val="frog" } } });
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
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

            ThenKVSIsConfigured(true);
            ThenKVSFilePathIs(Path.Combine(Application.persistentDataPath, "kvs.dat")); // As specified in MyLibraryConfig file
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
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void Save_CalledOnTeardown()
        {
            WhenKVSSetFloat("test", 7.6f);
            ThenKVSOnDiskMatches(new KVS.Data());

            WhenKVSDeinitialised();
            ThenKVSOnDiskMatches(new KVS.Data { floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="test", val=7.6f } } });
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void InitialLoadDeletesMalformedDiskData()
        {
            GivenKVSOnDisk("this is an invalid kvs data file", true);
            WhenKVSInitialised();
            ThenKVSIsNotOnDisk();
        }

        [Test]
        public void UnusableWhenNotConfigured()
        {
            // GIVEN no library config
            ThenKVSIsConfigured(false);
            ThenAllKVSOperationsFailForMissingConfig();
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigInvalidFilename")]
        public void UnusableWhenMisconfigured()
        {
            ThenKVSIsConfigured(false);
            ThenAllKVSOperationsFailForMissingConfig();
        }
        
        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigDifferentFilename")]
        public void DifferentFilename()
        {
            // GIVEN a blank slate
            ThenKVSIsNotOnDisk();
            ThenKVSIsConfigured(true);

            WhenKVSSetFloat("test", 7.6f);
            WhenKVSSaved();
            ThenKVSFilePathIs(Path.Combine(Application.persistentDataPath, "something-else.sav")); // As specified in MyLibraryConfig file
            ThenKVSOnDiskMatches(new KVS.Data { floats=new List<KVS.KVFloat> { new KVS.KVFloat { key="test", val=7.6f } } });
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void GetRawData_FromABlankSlate()
        {
            // GIVEN a blank slate
            ThenKVSGetRawDataMatches(new KVS.Data());
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void GetRawData_FromDisk()
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
            ThenKVSGetRawDataMatches(new KVS.Data {
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
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void GetRawData_WithUpdates()
        {
            // GIVEN a blank slate
            ThenKVSGetRawDataMatches(new KVS.Data());

            WhenKVSSetFloat("float1", -1f);
            WhenKVSSetFloat("float3", -40f);
            WhenKVSSetInt("int2", -12);
            WhenKVSSetInt("int4", -22);
            WhenKVSSetString("string1", "one is back");
            WhenKVSSetString("string3", "threeeee");
            ThenKVSGetRawDataMatches(new KVS.Data {
                floats=new List<KVS.KVFloat> { 
                    new KVS.KVFloat { key="float1", val=-1f },
                    new KVS.KVFloat { key="float3", val=-40f },
                },
                ints=new List<KVS.KVInt> {
                    new KVS.KVInt { key="int2", val=-12 },
                    new KVS.KVInt { key="int4", val=-22 },
                },
                strs=new List<KVS.KVString> {
                    new KVS.KVString { key="string1", val="one is back" },
                    new KVS.KVString { key="string3", val="threeeee" },
                },
            });

            WhenKVSDeleteKey("float1");
            WhenKVSDeleteKey("int2");
            WhenKVSDeleteKey("string1");
            ThenKVSGetRawDataMatches(new KVS.Data {
                floats=new List<KVS.KVFloat> { 
                    new KVS.KVFloat { key="float3", val=-40f },
                },
                ints=new List<KVS.KVInt> {
                    new KVS.KVInt { key="int4", val=-22 },
                },
                strs=new List<KVS.KVString> {
                    new KVS.KVString { key="string3", val="threeeee" },
                },
            });
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void SetRawData()
        {
            // GIVEN a blank slate
            WhenKVSSetRawData(new KVS.Data {
                floats=new List<KVS.KVFloat> { 
                    new KVS.KVFloat { key="float1", val=-100f },
                }
            });
            WhenKVSSetRawData(new KVS.Data {
                floats=new List<KVS.KVFloat> { 
                    new KVS.KVFloat { key="float1", val=-1f },
                    new KVS.KVFloat { key="float3", val=-40f },
                },
                ints=new List<KVS.KVInt> {
                    new KVS.KVInt { key="int2", val=-12 },
                    new KVS.KVInt { key="int4", val=-22 },
                },
                strs=new List<KVS.KVString> {
                    new KVS.KVString { key="string1", val="one is back" },
                    new KVS.KVString { key="string3", val="threeeee" },
                },
            });
            ThenKVSGetRawDataMatches(new KVS.Data {
                floats=new List<KVS.KVFloat> { 
                    new KVS.KVFloat { key="float1", val=-1f },
                    new KVS.KVFloat { key="float3", val=-40f },
                },
                ints=new List<KVS.KVInt> {
                    new KVS.KVInt { key="int2", val=-12 },
                    new KVS.KVInt { key="int4", val=-22 },
                },
                strs=new List<KVS.KVString> {
                    new KVS.KVString { key="string1", val="one is back" },
                    new KVS.KVString { key="string3", val="threeeee" },
                },
            });

            WhenKVSDeleteKey("float1");
            WhenKVSDeleteKey("int2");
            WhenKVSDeleteKey("string1");
            WhenKVSSetRawData(new KVS.Data {
                floats=new List<KVS.KVFloat> { 
                    new KVS.KVFloat { key="float1", val=-1f },
                },
                ints=new List<KVS.KVInt> {
                    new KVS.KVInt { key="int2", val=-12 },
                },
                strs=new List<KVS.KVString> {
                    new KVS.KVString { key="string1", val="one is back" },
                },
            });
            ThenKVSGetRawDataMatches(new KVS.Data {
                floats=new List<KVS.KVFloat> { 
                    new KVS.KVFloat { key="float1", val=-1f },
                },
                ints=new List<KVS.KVInt> {
                    new KVS.KVInt { key="int2", val=-12 },
                },
                strs=new List<KVS.KVString> {
                    new KVS.KVString { key="string1", val="one is back" },
                },
            });
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void SetRawData_FromDisk()
        {
            GivenKVSOnDisk(new KVS.Data {
                floats=new List<KVS.KVFloat> { 
                    new KVS.KVFloat { key="float1", val=float.MaxValue },
                },
                ints=new List<KVS.KVInt> {
                    new KVS.KVInt { key="int1", val=400 },
                    new KVS.KVInt { key="int2", val=int.MinValue },
                },
                strs=new List<KVS.KVString> {
                    new KVS.KVString { key="string1", val="one" },
                    new KVS.KVString { key="string4", val="fou4" },
                },
            });
            WhenKVSSetRawData(new KVS.Data {
                floats=new List<KVS.KVFloat> { 
                    new KVS.KVFloat { key="float1", val=-100f },
                },
            });
            ThenKVSGetRawDataMatches(new KVS.Data {
                floats=new List<KVS.KVFloat> { 
                    new KVS.KVFloat { key="float1", val=-100f },
                },
            });
        }

        [Test]
        [GivenMyLibraryConfig(ASSET_DIR + "/TestConfigStandard")]
        public void OnSaveEvent()
        {
            WhenKVSSetFloat("test", 5.2f);
            WhenKVSSetFloat("test2", -13.1f);
            WhenKVSDeleteKey("test2");
            ThenKVSOnSaveIsTriggeredOnce(WhenKVSSaved);

            WhenKVSDeleteKey("test");
            ThenKVSOnSaveIsTriggeredOnce(WhenKVSSaved);

            WhenKVSSetRawData(new KVS.Data {
                floats=new List<KVS.KVFloat> { 
                    new KVS.KVFloat { key="float1", val=float.MaxValue },
                },
                ints=new List<KVS.KVInt> {
                    new KVS.KVInt { key="int1", val=400 },
                    new KVS.KVInt { key="int2", val=int.MinValue },
                },
                strs=new List<KVS.KVString> {
                    new KVS.KVString { key="string1", val="one" },
                    new KVS.KVString { key="string4", val="fou4" },
                },
            });
            ThenKVSOnSaveIsTriggeredOnce(WhenKVSDeinitialised);
        }
        
        void GivenKVSOnDisk(KVS.Data data) =>
            GivenKVSOnDisk(JsonUtility.ToJson(data));
        
        void GivenKVSOnDisk(string data, bool isInvalid=false)
        {
            KVS.Deinit();

            if (isInvalid)
                LogAssert.Expect(
                    LogType.Error,
                    new Regex("^Failed to convert saved data from JSON, nuking!")
                );
            File.WriteAllText(KVS.FilePath, data);

            KVS.Init();
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
        
        void WhenKVSSetRawData(KVS.Data data) =>
            KVS.RawData = data;
        
        void ThenKVSIsConfigured(bool expected) =>
            Assert.AreEqual(expected, KVS.Configured);
        
        void ThenKVSGetFloatEquals(string key, float expected, float defaultValue=0.0f) =>
            Assert.AreEqual(expected, KVS.GetFloat(key, defaultValue));
        
        void ThenKVSGetIntEquals(string key, int expected, int defaultValue=0) =>
            Assert.AreEqual(expected, KVS.GetInt(key, defaultValue));
        
        void ThenKVSGetStringEquals(string key, string expected, string defaultValue="") =>
            Assert.AreEqual(expected, KVS.GetString(key, defaultValue));
        
        void ThenKVSHasKey(string key, bool expected) =>
            Assert.AreEqual(expected, KVS.HasKey(key));
        
        void ThenAllKVSOperationsFailForMissingConfig()
        {
            var ops = new TestDelegate[]
            {
                KVS.DeleteAll,
                () => KVS.DeleteKey("key"),
                () => KVS.GetFloat("key"),
                () => KVS.GetInt("key"),
                () => KVS.GetString("key"),
                () => KVS.HasKey("key"),
                KVS.Save,
                () => KVS.SetFloat("key", 0f),
                () => KVS.SetInt("key", 0),
                () => KVS.SetString("key", ""),
            };

            foreach (var op in ops)
            {
                var ex = Assert.Throws<InvalidOperationException>(op);
                Assert.AreEqual("Cannot use KVS without setting up MyLibraryConfig.kvs", ex.Message);
            }
        }

        void ThenKVSFilePathIs(string expected) =>
            Assert.AreEqual(expected, KVS.FilePath);
        
        void ThenKVSOnDiskMatches(KVS.Data expected)
        {
            var json = File.Exists(KVS.FilePath)
                ? File.ReadAllText(KVS.FilePath)
                : "{}";
            var actual = JsonUtility.FromJson<KVS.Data>(json);

            AssertKVSDataIsEqual(actual, expected);
        }

        void AssertKVSDataIsEqual(KVS.Data actual, KVS.Data expected)
        {
            Assert.AreEqual(expected.floats.Count, actual.floats.Count);
            for (var i = 0; i != actual.floats.Count; ++i)
            {
                Assert.AreEqual(expected.floats[i].key, actual.floats[i].key);
                Assert.IsTrue(Mathf.Approximately(
                    expected.floats[i].val,
                    actual.floats[i].val
                ));
            }

            Assert.AreEqual(expected.ints.Count, actual.ints.Count);
            for (var i = 0; i != actual.ints.Count; ++i)
            {
                Assert.AreEqual(expected.ints[i].key, actual.ints[i].key);
                Assert.AreEqual(expected.ints[i].val, actual.ints[i].val);
            }

            Assert.AreEqual(expected.strs.Count, actual.strs.Count);
            for (var i = 0; i != actual.strs.Count; ++i)
            {
                Assert.AreEqual(expected.strs[i].key, actual.strs[i].key);
                Assert.AreEqual(expected.strs[i].val, actual.strs[i].val);
            }
        }

        void ThenKVSIsNotOnDisk() =>
            Assert.IsFalse(File.Exists(KVS.FilePath));
        
        void ThenKVSGetRawDataMatches(KVS.Data expected) =>
            AssertKVSDataIsEqual(KVS.RawData, expected);
        
        void ThenKVSOnSaveIsTriggeredOnce(Action whenThisHappens)
        {
            var triggerCount = 0;
            Action onSaveAction = () => ++triggerCount;

            KVS.onSave += onSaveAction;
            whenThisHappens();
            KVS.onSave -= onSaveAction;

            Assert.AreEqual(1, triggerCount);
        }
    }
}
