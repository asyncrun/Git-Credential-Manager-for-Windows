﻿/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.Collections.Generic;
using Moq;
using Xunit;

namespace Microsoft.Alm.Cli.Test
{
    public class ProgramTests
    {
        [Fact]
        public void LoadOperationArgumentsTest()
        {
            var program = new Program
            {
                _dieException = (Program caller, Exception e, string path, int line, string name) => Assert.False(true, $"Error: {e.ToString()}"),
                _dieMessage = (Program caller, string m, string path, int line, string name) => Assert.False(true, $"Error: {m}"),
                _exit = (Program caller, int e, string m, string path, int line, string name) => Assert.False(true, $"Error: {e} {m}")
            };

            var configs = new Dictionary<Git.ConfigurationLevel, Dictionary<string, string>>
            {
                {
                    Git.ConfigurationLevel.Local,
                    new Dictionary<string, string>(Program.ConfigKeyComparer)
                    {
                        { "credential.validate", "true" },
                        { "credential.useHttpPath", "true" },
                        { "credential.not-match.com.useHttpPath", "false" },
                    }
                },
                {
                    Git.ConfigurationLevel.Global,
                    new Dictionary<string, string>(Program.ConfigKeyComparer)
                    {
                        { "credential.validate", "false" },
                    }
                },
                {
                    Git.ConfigurationLevel.Xdg,
                    new Dictionary<string, string>(Program.ConfigKeyComparer) { }
                },
                {
                    Git.ConfigurationLevel.System,
                    new Dictionary<string, string>(Program.ConfigKeyComparer) { }
                },
                {
                    Git.ConfigurationLevel.Portable,
                    new Dictionary<string, string>(Program.ConfigKeyComparer) { }
                },
            };
            var envvars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "HOME", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) },
            };
            var gitconfig = new Git.Configuration.Impl(configs);
            var targetUri = new Authentication.TargetUri("https://example.visualstudio.com/");

            var opargsMock = new Mock<OperationArguments>(MockBehavior.Strict);
            opargsMock.Setup(r => r.EnvironmentVariables)
                      .Returns(envvars);
            opargsMock.Setup(r => r.GitConfiguration)
                      .Returns(gitconfig);
            opargsMock.Setup(r => r.LoadConfiguration());
            opargsMock.Setup(r => r.TargetUri)
                      .Returns(targetUri);
            opargsMock.Setup(r => r.QueryUri)
                      .Returns(targetUri);
            opargsMock.SetupProperty(r => r.UseHttpPath);
            opargsMock.SetupProperty(r => r.ValidateCredentials);

            var opargs = opargsMock.Object;

            program.LoadOperationArguments(opargs);

            Assert.NotNull(opargs);
            Assert.True(opargs.ValidateCredentials, "credential.validate");
            Assert.True(opargs.UseHttpPath, "credential.useHttpPath");
        }

        public static object[][] TryReadBooleanData
        {
            get
            {
                var data = new List<object[]>();
                var trueValues = new[] { "true", "1", "on", "yes", };
                var falseValues = new[] { "false", "0", "off", "no", };

                foreach (KeyType key in Enum.GetValues(typeof(KeyType)))
                {
                    foreach (var value in trueValues)
                    {
                        var upper = value.ToUpper();

                        data.Add(new object[] { (int)key, value, null, true });
                        data.Add(new object[] { (int)key, null, value, true });

                        if (StringComparer.Ordinal.Equals(upper, value))
                        {
                            data.Add(new object[] { (int)key, upper, null, true });
                            data.Add(new object[] { (int)key, null, upper, true });
                        }
                    }

                    foreach (var value in falseValues)
                    {
                        var upper = value.ToUpper();

                        data.Add(new object[] { (int)key, value, null, false });
                        data.Add(new object[] { (int)key, null, value, false });

                        if (StringComparer.Ordinal.Equals(upper, value))
                        {
                            data.Add(new object[] { (int)key, upper, null, false });
                            data.Add(new object[] { (int)key, null, upper, false });
                        }
                    }
                }

                return data.ToArray();
            }
        }

        [Theory, MemberData(nameof(TryReadBooleanData), DisableDiscoveryEnumeration = true)]
        public void TryReadBooleanTest(int keyValue, string configValue, string environValue, bool? expectedValue)
        {
            bool? yesno;
            KeyType key = (KeyType)keyValue;

            var program = new Program
            {
                _dieException = (Program caller, Exception e, string path, int line, string name) => Assert.False(true, $"Error: {e.ToString()}"),
                _dieMessage = (Program caller, string m, string path, int line, string name) => Assert.False(true, $"Error: {m}"),
                _exit = (Program caller, int e, string m, string path, int line, string name) => Assert.False(true, $"Error: {e} {m}")
            };

            Assert.True(program.EnvironmentKeys.ContainsKey(key));

            var configs = new Dictionary<Git.ConfigurationLevel, Dictionary<string, string>>
            {
                { Git.ConfigurationLevel.Local,    new Dictionary<string, string>(Program.ConfigKeyComparer) },
                { Git.ConfigurationLevel.Global,   new Dictionary<string, string>(Program.ConfigKeyComparer) },
                { Git.ConfigurationLevel.Xdg,      new Dictionary<string, string>(Program.ConfigKeyComparer) },
                { Git.ConfigurationLevel.System,   new Dictionary<string, string>(Program.ConfigKeyComparer) },
                { Git.ConfigurationLevel.Portable, new Dictionary<string, string>(Program.ConfigKeyComparer) },
            };
            var envvars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "HOME", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) },
            };

            bool setupComplete = false;

            if (!string.IsNullOrEmpty(configValue) && program.ConfigurationKeys.TryGetValue(key, out string configKey))
            {
                configs[Git.ConfigurationLevel.Local].Add($"credential.{configKey}", configValue);
                setupComplete = true;
            }

            if (!string.IsNullOrEmpty(environValue) && program.EnvironmentKeys.TryGetValue(key, out string environKey))
            {
                envvars.Add(environKey, environValue);
                setupComplete = true;
            }

            if (!setupComplete)
                return;

            var gitconfig = new Git.Configuration.Impl(configs);
            var targetUri = new Authentication.TargetUri("https://example.visualstudio.com/");

            var opargsMock = new Mock<OperationArguments>();
            opargsMock.Setup(v => v.EnvironmentVariables)
                      .Returns(envvars);
            opargsMock.Setup(v => v.GitConfiguration)
                      .Returns(gitconfig);
            opargsMock.Setup(v => v.LoadConfiguration());
            opargsMock.Setup(v => v.TargetUri)
                      .Returns(targetUri);
            opargsMock.Setup(v => v.QueryUri)
                      .Returns(targetUri);

            if (expectedValue.HasValue)
            {
                Assert.True(CommonFunctions.TryReadBoolean(program, opargsMock.Object, key, out yesno));
                Assert.Equal(expectedValue, yesno);
            }
            else
            {
                Assert.False(CommonFunctions.TryReadBoolean(program, opargsMock.Object, key, out yesno));
                Assert.False(yesno.HasValue);
            }
        }
    }
}
