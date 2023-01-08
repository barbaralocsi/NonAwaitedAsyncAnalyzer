﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = MakeConst.Test.CSharpCodeFixVerifier<
    MakeConst.MakeConstAnalyzer,
    MakeConst.MakeConstCodeFixProvider>;

namespace MakeConst.Test
{
    [TestClass]
    public class MakeConstUnitTest
    {


        [TestMethod]
        public async Task AwaitedAsyncTask_NoDiagnostic()
        {
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Threading.Tasks;

class Program
{
    public static async Task Main()
    {
        var a = await GetValueAsync(1);
        Console.WriteLine(a);
    }

    private static async Task<int> GetValueAsync(int numberToAdd)
    {
        return await Task.Run(() => numberToAdd * 2);
    }
}

");
        }

        [TestMethod]
        public async Task NonAwaitedAsyncTask_Diagnostic()
        {

            var expected = VerifyCS.Diagnostic("MakeConst").WithMessage("Async call should be awaited").WithSpan(9, 17, 9, 33);
            await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using System.Threading.Tasks;

class Program
{
    public static async Task Main()
    {
        var a = GetValueAsync(1);
        Console.WriteLine(a);
    }

    private static async Task<int> GetValueAsync(int numberToAdd)
    {
        return await Task.Run(() => numberToAdd * 2);
    }
}

", expected);
        }

        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class {|#0:TypeName|}
        {   
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic("MakeConst").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
