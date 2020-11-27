﻿using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UsertypeDefTools
{
	class CSharpCodeExport
	{
		public static readonly string FileName = "UserTypes.cs";
		readonly string Head =
@"// DO NOT modify this file directly. It was automatically generated by UserTypeDefTools.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KBEngine
{";

		readonly string Tail = @"}";

		private List<BaseType> m_types;

		public CSharpCodeExport(List<BaseType> types)
		{
			m_types = types;

		}


		internal bool Save(string dir)
		{
			var path = Path.Combine( dir, FileName );
			StringBuilder sb = new StringBuilder();

			#region Head
			sb.AppendLine( @"// DO NOT modify this file directly. It was automatically generated by UserTypeDefTools." );
			sb.AppendLine( @"using System;" );
			sb.AppendLine( @"using System.Collections.Generic;" );
			sb.AppendLine( @"using System.Linq;" );
			sb.AppendLine( @"using System.Text;" );
			sb.AppendLine();
			sb.AppendLine( @"namespace KBEngine" );
			sb.AppendLine( @"{" );
			#endregion
			//begin

			//type
			sb.AppendLine( "#region UserTypes" );
			sb.AppendLine();
			foreach( var item in m_types )
			{
				var userType = item as UserType;
				if( userType != null )
				{
					sb.AppendFormat( "#region {0}", userType.TypeName ); sb.AppendLine(); sb.AppendLine();

					sb.AppendLine( userType.GetClassCode() );

					sb.AppendLine(); sb.AppendLine( "#endregion" ); sb.AppendLine();
				}
			}
			sb.AppendLine( "#endregion" );

			sb.AppendLine();

			//helper
			sb.AppendLine( "#region Helper" ); sb.AppendLine();

			sb.AppendLine( "public static class TypeCastHelper\n\t{" );

			foreach( var item in m_types )
			{
				var userType = item as UserType;
				if( userType != null )
					sb.AppendLine( userType.GetHelperFunCode() );
			}

			sb.AppendLine( "\t}" );
			sb.AppendLine();

			sb.AppendLine( "#endregion" );

			//end

			sb.AppendLine( Tail );
			File.WriteAllText( path, sb.ToString(), Encoding.UTF8 );

			return true;
		}
	}

	public static class TypeExportHelper
	{
		public static string GetClassCode(this UserType type)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append( "\tpublic class " );
			sb.AppendLine( type.TypeName );
			sb.AppendLine( "\t{" );
			foreach( var item in type.Properties )
				sb.AppendLine( item.GetFileCode() );
			sb.Append( "\t}" );
			return sb.ToString();
		}

		public static string GetFileCode(this UserType.Field filed)
		{
			string typeStr = filed.Type.GetCSharpTypeString();

			return string.Format( "\t\tpublic {0} {1};", typeStr, filed.FieldName );
		}

		public static string GetHelperFunCode(this UserType type)
		{
			var typeName = type.TypeName;
			StringBuilder sb = new StringBuilder();
			sb.Append( "\t\t" ); sb.AppendFormat( "public static {0} Get_{0}(this Dictionary<string, object> dict)", typeName ); sb.AppendLine();
			sb.AppendLine( "\t\t{" );

			sb.Append( "\t\t\t" ); sb.AppendFormat( "var ret = new {0}();", typeName ); sb.AppendLine();

			foreach( var field in type.Properties )
			{
				sb.Append( "\t\t\t" );
				if( field.Type is UserType )
				{
					sb.AppendFormat( "ret.{0} = ((Dictionary<string, object>)dict[\"data\"]).Get_{1}();", field.FieldName, field.Type.GetCSharpTypeString() ); sb.AppendLine();
				}
				else if( field.Type is ArrayType )
				{
					var arrayType = (ArrayType)field.Type;
					sb.AppendFormat( "ret.{0} = (from av in (List<object>)dict[\"{0}\"] select ( {1} ) ).ToList();", field.FieldName, GetArrayTypeHelperFunCode( "av", arrayType ) ); sb.AppendLine();
				}
				else
				{
					sb.AppendFormat( "ret.{0} = ({1})dict[\"{0}\"];", field.FieldName, field.Type.GetCSharpTypeString() ); sb.AppendLine();
				}
			}

			sb.Append( "\t\t\t" ); sb.AppendLine( "return ret;" );
			sb.AppendLine( "\t\t}" );
			return sb.ToString();
		}

		static int VariablesIndex = 1;
		static string GetArrayTypeHelperFunCode(string collection, ArrayType type)
		{
			StringBuilder sb = new StringBuilder();

			if( type.ElementType is UserType )
			{
				sb.AppendFormat( "( (Dictionary<string, object>){0} ).Get_{1}()", collection, type.ElementType.GetCSharpTypeString() );
			}
			else if( type.ElementType is ArrayType )
			{
				sb.AppendFormat( "( from tempE_{0} in (List<object>){1} select ( {2} ) ).ToList()", ++VariablesIndex, collection, GetArrayTypeHelperFunCode( "tempE_" + VariablesIndex.ToString(), type.ElementType as ArrayType ) );
			}
			else
				sb.AppendFormat( "({0}){1}", type.ElementType.GetCSharpTypeString(), collection );
			return sb.ToString();
		}
	}
}
