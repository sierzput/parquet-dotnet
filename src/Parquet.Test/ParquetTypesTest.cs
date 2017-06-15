using System;
using System.Collections;
using System.IO;
using System.Numerics;
using System.Reflection;
using Xunit;

namespace Parquet.Test
{
    public class ParquetTypesTest
    {
       [Fact]
       public void TestInt32()
       {
          var utils = new NumericUtils();
          byte[] parquetBlock = utils.IntToLittleEndian(999);

          var encoding = new ParquetEncoding();
          var output = encoding.ReadPlain<int>(parquetBlock, Thrift.Type.INT32, 3);

          Assert.Equal(999, output);
       }

       [Fact]
       public void TestInt64()
       {
          var utils = new NumericUtils();
          byte[] parquetBlock = utils.LongToLittleEndian(9999);

          var encoding = new ParquetEncoding();
          var output = encoding.ReadPlain<long>(parquetBlock, Thrift.Type.INT64, 4);

          Assert.Equal(9999, output);
       }

       [Fact]
       public void TestFloat()
       {
          var utils = new NumericUtils();
          byte[] parquetBlock = utils.FloatToLittleEndian(9999.999f);

          var encoding = new ParquetEncoding();
          var output = encoding.ReadPlain<float>(parquetBlock, Thrift.Type.FLOAT, 8);

          Assert.Equal(9999.999f, output);
       }

       [Fact]
       public void TestDouble()
       {
          var utils = new NumericUtils();
          byte[] parquetBlock = utils.DoubleToLittleEndian(9999.9999999D);

          var encoding = new ParquetEncoding();
          var output = encoding.ReadPlain<double>(parquetBlock, Thrift.Type.DOUBLE, 12);

          Assert.Equal(9999.9999999D, output);
       }

       [Fact]
       public void TestBoolean()
       {
          var boolArray = new bool[] {true, true, false, true};
          var bitArray = new BitArray(boolArray);

          var encoding = new ParquetEncoding();
          var output = encoding.ReadPlain<bool[]>(new byte[] {bitArray.GetByte()}, Thrift.Type.BOOLEAN, 4);

          Assert.Equal(boolArray, output);
      }

       [Fact]
       public void TestInt96()
       {
          byte[] parquetBlock = new byte[] { 255, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
         
          var encoding = new ParquetEncoding();
          var output = encoding.ReadPlain<BigInteger>(parquetBlock, Thrift.Type.INT96, 1);

          var bi = new BigInteger(65535D);

          Assert.Equal(bi, output);
       }

       [Fact]
       public void TestByteArray()
       {
          byte[] parquetBlock = new byte[] { 1, 0, 0, 0, 67, 68, 69, 70 };

          var encoding = new ParquetEncoding();
          var output = encoding.ReadPlain<byte[]>(parquetBlock, Thrift.Type.BYTE_ARRAY, 1);

          Assert.Equal(new byte[] { 67 }, output);
       }

      private string GetDataFilePath(string name)
        {
            string thisPath = Assembly.Load(new AssemblyName("Parquet.Test")).Location;
            return Path.Combine(Path.GetDirectoryName(thisPath), "data", name);
        }
    }
}