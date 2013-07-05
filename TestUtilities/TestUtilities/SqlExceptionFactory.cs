// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SqlExceptionFactory.cs" company="Rare Crowds Inc">
// Copyright 2012-2013 Rare Crowds, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Security;
using System.Xml;

namespace TestUtilities
{
    /// <summary>Creates instances of System.Data.SqlClient.SqlException</summary>
    public static class SqlExceptionFactory
    {
        /// <summary>Format string for creating SqlException XML</summary>
        /// <remarks>
        /// 0: ExceptionMessage  string
        /// 1: StackTrace        string
        /// 2: ErrorClass        byte
        /// 3: LineNumber        int
        /// 4: ErrorMessage      string
        /// 5: ErrorNumber       int
        /// 6: Procedure         string
        /// 7: Server            string
        /// 8: State             byte
        /// </remarks>
        private const string SqlExceptionXmlFormat = @"
<SqlException xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:x=""http://www.w3.org/2001/XMLSchema"" xmlns=""http://schemas.datacontract.org/2004/07/System.Data.SqlClient"">
  <Errors xmlns:d2p1=""http://schemas.datacontract.org/2004/07/System.Data.SqlClient"" i:type=""d2p1:SqlErrorCollection"" xmlns="""">
    <d2p1:errors xmlns:d3p1=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
      <d3p1:anyType i:type=""d2p1:SqlError"">
        <d2p1:errorClass>{2}</d2p1:errorClass>
        <d2p1:lineNumber>{3}</d2p1:lineNumber>
        <d2p1:message>{4}</d2p1:message>
        <d2p1:number>{5}</d2p1:number>
        <d2p1:procedure>{6}</d2p1:procedure>
        <d2p1:server>{7}</d2p1:server>
        <d2p1:source>.Net SqlClient Data Provider</d2p1:source>
        <d2p1:state>{8}</d2p1:state>
      </d3p1:anyType>
    </d2p1:errors>
  </Errors>
  <ClassName i:type=""x:string"" xmlns="""">System.Data.SqlClient.SqlException</ClassName>
  <Message i:type=""x:string"" xmlns="""">{0}</Message>
  <Data xmlns:d2p1=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" i:type=""d2p1:ArrayOfKeyValueOfanyTypeanyType"" xmlns="""">
    <d2p1:KeyValueOfanyTypeanyType>
      <d2p1:Key i:type=""x:string"">HelpLink.ProdName</d2p1:Key>
      <d2p1:Value i:type=""x:string"">Microsoft SQL Server</d2p1:Value>
    </d2p1:KeyValueOfanyTypeanyType>
    <d2p1:KeyValueOfanyTypeanyType>
      <d2p1:Key i:type=""x:string"">HelpLink.ProdVer</d2p1:Key>
      <d2p1:Value i:type=""x:string"">10.00.2531</d2p1:Value>
    </d2p1:KeyValueOfanyTypeanyType>
    <d2p1:KeyValueOfanyTypeanyType>
      <d2p1:Key i:type=""x:string"">HelpLink.EvtSrc</d2p1:Key>
      <d2p1:Value i:type=""x:string"">MSSQLServer</d2p1:Value>
    </d2p1:KeyValueOfanyTypeanyType>
    <d2p1:KeyValueOfanyTypeanyType>
      <d2p1:Key i:type=""x:string"">HelpLink.EvtID</d2p1:Key>
      <d2p1:Value i:type=""x:string"">2812</d2p1:Value>
    </d2p1:KeyValueOfanyTypeanyType>
    <d2p1:KeyValueOfanyTypeanyType>
      <d2p1:Key i:type=""x:string"">HelpLink.BaseHelpUrl</d2p1:Key>
      <d2p1:Value i:type=""x:string"">http://go.microsoft.com/fwlink</d2p1:Value>
    </d2p1:KeyValueOfanyTypeanyType>
    <d2p1:KeyValueOfanyTypeanyType>
      <d2p1:Key i:type=""x:string"">HelpLink.LinkId</d2p1:Key>
      <d2p1:Value i:type=""x:string"">20476</d2p1:Value>
    </d2p1:KeyValueOfanyTypeanyType>
  </Data>
  <InnerException i:nil=""true"" xmlns="""" />
  <HelpURL i:nil=""true"" xmlns="""" />
  <StackTraceString i:type=""x:string"" xmlns="""">{1}</StackTraceString>
  <RemoteStackTraceString i:nil=""true"" xmlns="""" />
  <RemoteStackIndex i:type=""x:int"" xmlns="""">0</RemoteStackIndex>
  <ExceptionMethod i:type=""x:string"" xmlns="""">8\nOnError\nSystem.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\nSystem.Data.SqlClient.SqlConnection\nVoid OnError(System.Data.SqlClient.SqlException, Boolean)</ExceptionMethod>
  <HResult i:type=""x:int"" xmlns="""">-2146232060</HResult>
  <Source i:type=""x:string"" xmlns="""">.Net SqlClient Data Provider</Source>
  <WatsonBuckets i:nil=""true"" xmlns="""" />
</SqlException>";

        /// <summary>Backing field for Serializer. DO NOT USE DIRECTLY.</summary>
        private static DataContractSerializer serializer;

        /// <summary>Gets the data contract serializer</summary>
        private static DataContractSerializer Serializer
        {
            get
            {
                if (serializer == null)
                {
                    var knownTypes = new[]
                    {
                        typeof(SqlErrorCollection),
                        typeof(SqlError),
                        typeof(System.Collections.IDictionary).Assembly.GetType("System.Collections.ListDictionaryInternal")
                    };

                    serializer = new DataContractSerializer(
                        typeof(SqlException),
                        knownTypes);
                }

                return serializer;
            }
        }

        /// <summary>Creates an instance of SqlException</summary>
        /// <param name="message">Exception message</param>
        /// <param name="errorClass">SqlError Class</param>
        /// <param name="lineNumber">SqlError LineNumber</param>
        /// <param name="errorMessage">SqlError Message</param>
        /// <param name="errorNumber">SqlError Number</param>
        /// <param name="procedure">SqlError Procedure</param>
        /// <param name="server">SqlError Server</param>
        /// <param name="state">SqlError State</param>
        /// <returns>The created SqlException</returns>
        public static SqlException Create(
            string message,
            byte errorClass,
            int lineNumber,
            string errorMessage,
            int errorNumber,
            string procedure,
            string server,
            byte state)
        {
            var stackTrace = new StackTrace()
                .ToString()
                .Replace("\r\n", "\\n");
            var sqlExceptionXml = string.Format(
                CultureInfo.InvariantCulture,
                SqlExceptionXmlFormat,
                SecurityElement.Escape(message),
                SecurityElement.Escape(stackTrace),
                errorClass,
                lineNumber,
                SecurityElement.Escape(errorMessage),
                errorNumber,
                SecurityElement.Escape(procedure),
                SecurityElement.Escape(server),
                state);
            using (var reader = new StringReader(sqlExceptionXml))
            {
                using (var xmlReader = new XmlTextReader(reader))
                {
                    return Serializer.ReadObject(xmlReader) as SqlException;
                }
            }
        }
    }
}
