﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Parquet.File.Values.Primitives;
using Parquet.Schema;

// MIGRATION LEFTOVERS

namespace Parquet.Data {

    abstract class BasicDataTypeHandler<TSystemType> : IDataTypeHandler {
        private readonly Thrift.Type _thriftType;
        private readonly Thrift.ConvertedType? _convertedType;
        private static readonly ArrayPool<int> IntPool = ArrayPool<int>.Shared;

        public BasicDataTypeHandler(DataType dataType, Thrift.Type thriftType, Thrift.ConvertedType? convertedType = null) {
            _thriftType = thriftType;
            _convertedType = convertedType;
        }

        public SchemaType SchemaType => SchemaType.Data;

        public virtual bool IsMatch(Thrift.SchemaElement tse, ParquetOptions formatOptions) {
            return
               tse.__isset.type && _thriftType == tse.Type &&
               (_convertedType == null || (tse.__isset.converted_type && tse.Converted_type == _convertedType.Value));
        }

        public virtual Array MergeDictionary(Array untypedDictionary, int[] indexes, Array data, int offset, int length) {
            TSystemType[] dictionary = (TSystemType[])untypedDictionary;
            TSystemType[] result = (TSystemType[])data;

            for(int i = 0; i < length; i++) {
                int index = indexes[i];
                if(index < dictionary.Length) {
                    // may not be true when value is null
                    TSystemType value = dictionary[index];
                    result[offset + i] = value;
                }
            }

            return result;
        }
    }

    abstract class BasicPrimitiveDataTypeHandler<TSystemType> : BasicDataTypeHandler<TSystemType>
  where TSystemType : struct {
        public BasicPrimitiveDataTypeHandler(DataType dataType, Thrift.Type thriftType, Thrift.ConvertedType? convertedType = null)
           : base(dataType, thriftType, convertedType) {
        }
    }

    class BooleanDataTypeHandler : BasicPrimitiveDataTypeHandler<bool> {
        public BooleanDataTypeHandler() : base(DataType.Boolean, Thrift.Type.BOOLEAN) { // T+
        }
    }

    class Int32DataTypeHandler : BasicPrimitiveDataTypeHandler<int> {
        public Int32DataTypeHandler() : base(DataType.Int32, Thrift.Type.INT32) { // T+
        }
    }

    class Int64DataTypeHandler : BasicPrimitiveDataTypeHandler<long> {
        public Int64DataTypeHandler() : base(DataType.Int64, Thrift.Type.INT64) { // T+
        }
    }

    class DoubleDataTypeHandler : BasicPrimitiveDataTypeHandler<double> {
        public DoubleDataTypeHandler() : base(DataType.Double, Thrift.Type.DOUBLE) {  // T+

        }
    }

    class FloatDataTypeHandler : BasicPrimitiveDataTypeHandler<float> {
        public FloatDataTypeHandler() : base(DataType.Float, Thrift.Type.FLOAT) { // T+
        }
    }

    class ByteArrayDataTypeHandler : BasicDataTypeHandler<byte[]> {
        private static readonly ArrayPool<byte[]> _byteArrayPool = ArrayPool<byte[]>.Shared;

        public ByteArrayDataTypeHandler() : base(DataType.ByteArray, Thrift.Type.BYTE_ARRAY) {  // T+
        }

        public override bool IsMatch(Thrift.SchemaElement tse, ParquetOptions formatOptions) {
            return tse.__isset.type && tse.Type == Thrift.Type.BYTE_ARRAY
                                    && !tse.__isset.converted_type;
        }
    }

    class ByteDataTypeHandler : BasicPrimitiveDataTypeHandler<byte> {
        public ByteDataTypeHandler() : base(DataType.Byte, Thrift.Type.INT32, Thrift.ConvertedType.UINT_8) { // T+

        }
    }

    class SignedByteDataTypeHandler : BasicPrimitiveDataTypeHandler<sbyte> {
        public SignedByteDataTypeHandler() : base(DataType.SignedByte, Thrift.Type.INT32, Thrift.ConvertedType.INT_8) { //T+

        }
    }

    class DateTimeDataTypeHandler : BasicPrimitiveDataTypeHandler<DateTime> {
        public DateTimeDataTypeHandler() : base(DataType.DateTimeOffset, Thrift.Type.BYTE_ARRAY) {

        }

        public override bool IsMatch(Thrift.SchemaElement tse, ParquetOptions formatOptions) => // T+
            (tse.Type == Thrift.Type.INT96 && formatOptions.TreatBigIntegersAsDates) || //Impala

            (tse.Type == Thrift.Type.INT64 && tse.__isset.converted_type &&
             tse.Converted_type is Thrift.ConvertedType.TIMESTAMP_MILLIS
                 or Thrift.ConvertedType.TIMESTAMP_MICROS) ||

            (tse.Type == Thrift.Type.INT32 && tse.__isset.converted_type &&
             tse.Converted_type == Thrift.ConvertedType.DATE);
    }

    class DateTimeOffsetDataTypeHandler : BasicPrimitiveDataTypeHandler<DateTimeOffset> {
        public DateTimeOffsetDataTypeHandler() : base(DataType.DateTimeOffset, Thrift.Type.INT96) {

        }

        public override bool IsMatch(Thrift.SchemaElement tse, ParquetOptions formatOptions) => // T+
            (tse.Type == Thrift.Type.INT96 && formatOptions.TreatBigIntegersAsDates) || //Impala

            (tse.Type == Thrift.Type.INT64 && tse.__isset.converted_type &&
             tse.Converted_type is Thrift.ConvertedType.TIMESTAMP_MILLIS
                 or Thrift.ConvertedType.TIMESTAMP_MICROS) ||

            (tse.Type == Thrift.Type.INT32 && tse.__isset.converted_type &&
             tse.Converted_type == Thrift.ConvertedType.DATE);
    }

    class Int96DataTypeHandler : BasicPrimitiveDataTypeHandler<BigInteger> {
        public Int96DataTypeHandler() : base(DataType.Int96, Thrift.Type.INT96) {
        }

        public override bool IsMatch(Thrift.SchemaElement tse, ParquetOptions formatOptions) { // T+
            return tse.Type == Thrift.Type.INT96 && !formatOptions.TreatBigIntegersAsDates;
        }
    }

    class Int16DataTypeHandler : BasicPrimitiveDataTypeHandler<short> {
        public Int16DataTypeHandler() : base(DataType.Int16, Thrift.Type.INT32, Thrift.ConvertedType.INT_16) { // T+

        }
    }

    class UnsignedInt16DataTypeHandler : BasicPrimitiveDataTypeHandler<ushort> {
        public UnsignedInt16DataTypeHandler() : base(DataType.UnsignedInt16, Thrift.Type.INT32, Thrift.ConvertedType.UINT_16) { // T+

        }
    }

    class DecimalDataTypeHandler : BasicPrimitiveDataTypeHandler<decimal> {
        public DecimalDataTypeHandler() : base(DataType.Decimal, Thrift.Type.FIXED_LEN_BYTE_ARRAY, Thrift.ConvertedType.DECIMAL) {
        }

        public override bool IsMatch(Thrift.SchemaElement tse, ParquetOptions formatOptions) { // T+
            return

               tse.__isset.converted_type && tse.Converted_type == Thrift.ConvertedType.DECIMAL &&

               (
                  tse.Type == Thrift.Type.FIXED_LEN_BYTE_ARRAY ||
                  tse.Type == Thrift.Type.INT32 ||
                  tse.Type == Thrift.Type.INT64
               );
        }
    }

    class IntervalDataTypeHandler : BasicPrimitiveDataTypeHandler<Interval> {
        public IntervalDataTypeHandler() : base(DataType.Interval, Thrift.Type.FIXED_LEN_BYTE_ARRAY, Thrift.ConvertedType.INTERVAL) { // T+

        }
    }

    class TimeSpanDataTypeHandler : BasicPrimitiveDataTypeHandler<TimeSpan> {
        public TimeSpanDataTypeHandler() : base(DataType.TimeSpan, Thrift.Type.INT64, Thrift.ConvertedType.TIME_MICROS) { // T+

        }

        public override bool IsMatch(Thrift.SchemaElement tse, ParquetOptions formatOptions) {
            return

               (tse.Type == Thrift.Type.INT64 && tse.__isset.converted_type && tse.Converted_type == Thrift.ConvertedType.TIME_MICROS) ||

               (tse.Type == Thrift.Type.INT32 && tse.__isset.converted_type && tse.Converted_type == Thrift.ConvertedType.TIME_MILLIS);
        }
    }

    class UnsignedInt32DataTypeHandler : BasicPrimitiveDataTypeHandler<uint> {
        public UnsignedInt32DataTypeHandler() : base(DataType.UnsignedInt32, Thrift.Type.INT32, Thrift.ConvertedType.UINT_32) { // T+

        }
    }

    class UnsignedInt64DataTypeHandler : BasicPrimitiveDataTypeHandler<ulong> {
        public UnsignedInt64DataTypeHandler() : base(DataType.UnsignedInt64, Thrift.Type.INT64, Thrift.ConvertedType.UINT_64) { // T+

        }
    }

    class StringDataTypeHandler : BasicDataTypeHandler<string> {
        public StringDataTypeHandler() : base(DataType.String, Thrift.Type.BYTE_ARRAY, Thrift.ConvertedType.UTF8) { // T+
        }

        public override bool IsMatch(Thrift.SchemaElement tse, ParquetOptions formatOptions) {
            return tse.__isset.type &&
               tse.Type == Thrift.Type.BYTE_ARRAY &&
               (
                  (tse.__isset.converted_type && tse.Converted_type == Thrift.ConvertedType.UTF8) ||
                  formatOptions.TreatByteArrayAsString
               );
        }
    }

    abstract class NonDataDataTypeHandler : IDataTypeHandler {
        public DataType DataType => DataType.Unspecified;

        public abstract SchemaType SchemaType { get; }

        public abstract bool IsMatch(Thrift.SchemaElement tse, ParquetOptions formatOptions);

        public Array MergeDictionary(Array dictionary, int[] indexes, Array data, int offset, int length) {
            throw new NotSupportedException();
        }
    }

    class ListDataTypeHandler : NonDataDataTypeHandler {
        public override SchemaType SchemaType => SchemaType.List;

        public override bool IsMatch(Thrift.SchemaElement tse, ParquetOptions formatOptions) {
            return tse.__isset.converted_type && tse.Converted_type == Thrift.ConvertedType.LIST;
        }
    }

    class MapDataTypeHandler : NonDataDataTypeHandler {
        public override SchemaType SchemaType => SchemaType.Map;

        public override bool IsMatch(Thrift.SchemaElement tse, ParquetOptions formatOptions) {
            return
               tse.__isset.converted_type &&
               (tse.Converted_type == Thrift.ConvertedType.MAP || tse.Converted_type == Thrift.ConvertedType.MAP_KEY_VALUE);
        }
    }

    class StructureDataTypeHandler : NonDataDataTypeHandler {
        public override SchemaType SchemaType => SchemaType.Struct;

        public override bool IsMatch(Thrift.SchemaElement tse, ParquetOptions formatOptions) {
            return
               tse.Num_children > 0;
        }
    }
}