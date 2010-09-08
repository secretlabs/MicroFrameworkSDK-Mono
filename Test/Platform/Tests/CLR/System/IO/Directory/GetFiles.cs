////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections;
using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Platform.Test;

using System.IO;

namespace Microsoft.SPOT.Platform.Tests
{
    public class GetFiles : IMFTestInterface
    {
        [SetUp]
        public InitializeResult Initialize()
        {
            // These tests rely on underlying file system so we need to make
            // sure we can format it before we start the tests.  If we can't
            // format it, then we assume there is no FS to test on this platform.

            // delete the directory DOTNETMF_FS_EMULATION
            try
            {
                IOTests.IntializeVolume();
                Directory.CreateDirectory(TestDir + "\\" + Mid1 + "\\" + Tail1);
                Directory.CreateDirectory(TestDir + "\\" + Mid1 + "\\" + Tail2);
                Directory.CreateDirectory(TestDir + "\\" + Mid2 + "\\" + Tail1);
                // [Dir] [file count]
                // TestDir 0
                // Mid1 0
                // Mid1/Tail1 4
                // Mid1/Tail2 1
                // Mid2 3
                // Mid2/Tail1 2
                AddFile(IOTests.Volume.RootDirectory + "\\" + TestDir + "\\" + Mid1 + "\\" + Tail1 + "\\TestFile1.txt");
                AddFile(IOTests.Volume.RootDirectory + "\\" + TestDir + "\\" + Mid1 + "\\" + Tail1 + "\\TestFile2.txt");
                AddFile(IOTests.Volume.RootDirectory + "\\" + TestDir + "\\" + Mid1 + "\\" + Tail1 + "\\FileTest1.txt");
                AddFile(IOTests.Volume.RootDirectory + "\\" + TestDir + "\\" + Mid1 + "\\" + Tail1 + "\\FileTest2.txt");
                AddFile(IOTests.Volume.RootDirectory + "\\" + TestDir + "\\" + Mid1 + "\\" + Tail2 + "\\TestFile1.txt");
                AddFile(IOTests.Volume.RootDirectory + "\\" + TestDir + "\\" + Mid2 + "\\HeadMidFile1.jpg");
                AddFile(IOTests.Volume.RootDirectory + "\\" + TestDir + "\\" + Mid2 + "\\TailMidFile2.txt");
                AddFile(IOTests.Volume.RootDirectory + "\\" + TestDir + "\\" + Mid2 + "\\TailMidFile3.exe");
                AddFile(IOTests.Volume.RootDirectory + "\\" + TestDir + "\\" + Mid2 + "\\" + Tail1 + "\\FileTest1.txt");
                AddFile(IOTests.Volume.RootDirectory + "\\" + TestDir + "\\" + Mid2 + "\\" + Tail1 + "\\FileTest2.txt");
            }
            catch (Exception ex)
            {
                Log.Exception("Skipping: Unable to initialize file system", ex);
                return InitializeResult.Skip;
            }

            return InitializeResult.ReadyToGo;
        }

        [TearDown]
        public void CleanUp()
        {
        }

        #region local vars
        private ArrayList validFiles = new ArrayList();
        private const string TestDir = "GetFiles";
        private const string Mid1 = "Mid1";
        private const string Mid2 = "Mid2";
        private const string Tail1 = "Tail1";
        private const string Tail2 = "Tail2";
        private char[] special = new char[] { '!', '#', '$', '%', '\'', '(', ')', '+', '-', '.', '@', '[', ']', '_', '`', '{', '}', '~' };
        #endregion local vars

        #region Helper functions
        private void AddFile(string path)
        {
            Log.Comment("Adding file: " + path);
            try
            {
                new FileInfo(path).Create().Close();
                validFiles.Add(path);
            }
            catch
            {
                Log.Exception("Failed to create file");
            }
        }
        private string StrArrayToStr(string[] list)
        {
            string result = "";
            foreach (string item in list)
            {
                result += item + ", ";
            }
            return result.TrimEnd(',', ' ');
        }

        private bool TestGetFiles(int expected, params string[] nodes)
        {
            return Verify(expected, Directory.GetFiles(GetPath(nodes)));
        }

        private bool Verify(int expected, string[] results)
        {
            bool valid = true;
            Log.Comment("Got " + StrArrayToStr(results));
            if (results.Length != expected)
            {
                Log.Exception("Expected " + expected + " files, got " + results.Length);
                valid = false;
            }
            foreach (string file in results)
            {
                if (!validFiles.Contains(file))
                {
                    Log.Exception("Unexpected file found: " + file);
                    valid = false;
                }
            }
            return valid;
        }

        private string GetPath(string[] nodes)
        {
            string path = "";
            if (nodes.Length > 0)
            {
                path = nodes[0];
                for (int i = 1; i < nodes.Length; i++)
                {
                    path += "\\" + nodes[i];
                }
            }
            Log.Comment("Path: " + path);
            return path;
        }

        private string RandFileName(int length)
        {
            char[] chars = new char[length];
            Random random = new Random();

            for (int i = 0; i < length; i++)
            {
                switch (random.Next(4))
                {
                    // 0 = specal chars
                    case 0:
                        int x = random.Next(special.Length);
                        chars[i] = special[x];
                        break;
                    // 1 = numbers
                    case 1:
                        chars[i] = (char)((int)'0' + random.Next(10));
                        break;
                    // 2 = upper case
                    case 2:
                        chars[i] = (char)((int)'A' + random.Next(26));
                        break;
                    // 3 = lower case
                    case 3:
                        chars[i] = (char)((int)'a' + random.Next(26));
                        break;
                    default:
                        throw new ArgumentException("Expected 0-3");
                }
            }
            return new string(chars);
        }
        #endregion Helper functions

        #region Test Cases

        [TestMethod]
        public MFTestResults InvalidArguments()
        {
            string[] dirs;
            MFTestResults result = MFTestResults.Pass;
            try
            {
                // null cases
                try
                {
                    Log.Comment("Null");
                    dirs = Directory.GetFiles(null);
                    result = MFTestResults.Fail;
                    Log.Exception("Expected ArgumentNullException, but got " + StrArrayToStr(dirs));
                }
                catch (ArgumentNullException) { /* pass case */ }

                // string.empty cases
                try
                {
                    Log.Comment("String.Empty");
                    dirs = Directory.GetFiles(String.Empty);
                    result = MFTestResults.Fail;
                    Log.Exception("Expected ArgumentException, but got " + StrArrayToStr(dirs));
                }
                catch (ArgumentException) { /* pass case */ }

                // whitespace cases
                try
                {
                    Log.Comment("White Space");
                    dirs = Directory.GetFiles("       ");
                    result = MFTestResults.Fail;
                    Log.Exception("Expected ArgumentException, but got " + StrArrayToStr(dirs));
                }
                catch (ArgumentException) { /* pass case */ }

                // New lines
                try
                {
                    Log.Comment("\\n");
                    dirs = Directory.GetFiles("\n");
                    result = MFTestResults.Fail;
                    Log.Exception("Expected ArgumentException, but got " + StrArrayToStr(dirs));
                }
                catch (ArgumentException) { /* pass case */ }

                // Filter as path
                try
                {
                    Log.Comment("*");
                    dirs = Directory.GetFiles("*");
                    result = MFTestResults.Fail;
                    Log.Exception("Expected ArgumentException, but got " + StrArrayToStr(dirs));
                }
                catch (ArgumentException) { /* pass case */ }
            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected exception: " + ex.Message);
                result = MFTestResults.Fail;
            }

            return result;
        }

        [TestMethod]
        public MFTestResults ValidCasesNoFilter()
        {
            MFTestResults result = MFTestResults.Pass;
            try
            {
                // relative
                Directory.SetCurrentDirectory(IOTests.Volume.RootDirectory);

                if (!TestGetFiles(0, "."))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(0, TestDir))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(0, TestDir, Mid1))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(4, TestDir, Mid1, Tail1))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(1, TestDir, Mid1, Tail2))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(3, TestDir, Mid2))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(2, TestDir, Mid2, Tail1))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(2, TestDir, Mid1, ".", Tail1, "..", Tail1, "..", "..", Mid2, Tail1))
                    result = MFTestResults.Fail;

                // Move up tree where there is more directories
                Directory.SetCurrentDirectory(TestDir + "\\" + Mid2);
                if (!TestGetFiles(3, "."))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(0, ".."))
                    result = MFTestResults.Fail;

                // absolute
                if (!TestGetFiles(4, IOTests.Volume.RootDirectory, TestDir, Mid1, Tail1))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(2, IOTests.Volume.RootDirectory, TestDir, Mid2, Tail1))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(3, IOTests.Volume.RootDirectory, TestDir, Mid2))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(0, IOTests.Volume.RootDirectory, TestDir, Mid1))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(0, IOTests.Volume.RootDirectory, TestDir))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(0, IOTests.Volume.RootDirectory))
                    result = MFTestResults.Fail;

                if (!TestGetFiles(2, IOTests.Volume.RootDirectory, TestDir, Mid1, ".", Tail1, "..", Tail1, "..", "..", Mid2, Tail1, "."))
                    result = MFTestResults.Fail;
            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected exception: " + ex.Message);
                result = MFTestResults.Fail;
            }

            return result;
        }

        [TestMethod]
        private MFTestResults RandomFileNames()
        {
            MFTestResults result = MFTestResults.Pass;
            try
            {
                Log.Comment("Light stress with 100 random file names");
                Random random = new Random();
                string randdir = IOTests.Volume.RootDirectory + "\\" + TestDir + "\\RandomFileNames";
                Directory.CreateDirectory(randdir);
                for (int i = 0; i < 100; i++)
                {
                    AddFile(randdir + "\\" + i + "_" + RandFileName(random.Next(100)));
                }
                if (!TestGetFiles(100, randdir))
                    result = MFTestResults.Fail;
            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected exception: " + ex.Message);
                result = MFTestResults.Fail;
            }

            return result;
        }

        [TestMethod]
        public MFTestResults SpecialFileNames()
        {
            MFTestResults result = MFTestResults.Pass;
            try
            {
                Log.Comment("Find file each with special char file names");
                string specialdir = IOTests.Volume.RootDirectory + "\\" + TestDir + "\\SpecialFileNames";
                Directory.CreateDirectory(specialdir);
                for (int i = 0; i < special.Length; i++)
                {
                    string file = i + "_" + new string(new char[] { special[i] }) + "_z.file";
                    AddFile(specialdir + "\\" + file);
                }
                if (!TestGetFiles(special.Length, specialdir))
                    result = MFTestResults.Fail;
            }
            catch (Exception ex)
            {
                Log.Exception("Unexpected exception: " + ex.Message);
                result = MFTestResults.Fail;
            }

            return result;
        }
        #endregion Test Cases
    }
}
