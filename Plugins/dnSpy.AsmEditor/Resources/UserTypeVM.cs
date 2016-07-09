﻿/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.ViewHelpers;
using dnSpy.Contracts.MVVM;
using dnSpy.Shared.Files.TreeView.Resources;
using dnSpy.Shared.MVVM;
using dnSpy.Shared.Search;

namespace dnSpy.AsmEditor.Resources {
	sealed class UserTypeVM : ViewModelBase {
		readonly bool canDeserialize;

		public IDnlibTypePicker DnlibTypePicker {
			set { dnlibTypePicker = value; }
		}
		IDnlibTypePicker dnlibTypePicker;

		public ICommand PickTypeCommand => new RelayCommand(a => PickType());

		public string TypeFullName {
			get { return typeFullName; }
			set {
				if (typeFullName != value) {
					typeFullName = value;
					OnPropertyChanged(nameof(TypeFullName));
					OnPropertyChanged(nameof(StringValue));
					HasErrorUpdated();
				}
			}
		}
		string typeFullName = string.Empty;

		public string StringValue {
			get { return stringValue; }
			set {
				if (stringValue != value) {
					stringValue = value;
					OnPropertyChanged(nameof(StringValue));
					HasErrorUpdated();
				}
			}
		}
		string stringValue = string.Empty;

		readonly ModuleDef ownerModule;

		public UserTypeVM(ModuleDef ownerModule, bool canDeserialize) {
			this.ownerModule = ownerModule;
			this.canDeserialize = canDeserialize;
		}

		void PickType() {
			if (dnlibTypePicker == null)
				throw new InvalidOperationException();
			var newType = dnlibTypePicker.GetDnlibType(dnSpy_AsmEditor_Resources.Pick_Type, new FlagsFileTreeNodeFilter(VisibleMembersFlags.TypeDef), GetTypeRef(), ownerModule);
			if (newType != null)
				TypeFullName = newType.AssemblyQualifiedName;
		}

		public void SetData(byte[] data) => StringValue = GetString(data);

		public byte[] GetSerializedData() {
			object obj;
			if (!string.IsNullOrEmpty(GetSerializedData(out obj)))
				return null;
			return SerializationUtils.Serialize(obj);
		}

		string GetString(byte[] data) {
			if (!canDeserialize)
				return string.Empty;

			if (data == null)
				return string.Empty;

			object obj;
			if (!string.IsNullOrEmpty(SerializationUtils.Deserialize(data, out obj)))
				return string.Empty;

			return SerializationUtils.ConvertObjectToString(obj);
		}

		string GetSerializedData(out object obj) {
			obj = null;
			Type type;
			var error = LoadType(out type);
			if (!string.IsNullOrEmpty(error))
				return error;

			return SerializationUtils.CreateObjectFromString(type, StringValue, out obj);
		}

		string LoadType(out Type type) {
			if (!canDeserialize) {
				type = null;
				return dnSpy_AsmEditor_Resources.Error_DeSerializationDisabledInSettings;
			}

			try {
				type = Type.GetType(TypeFullName);
				if (type == null)
					return dnSpy_AsmEditor_Resources.Error_CouldNotFindTypeOrItsAssembly;
				return string.Empty;
			}
			catch (Exception ex) {
				type = null;
				return string.Format(dnSpy_AsmEditor_Resources.Error_CouldNotLoadType, typeFullName, ex.Message);
			}
		}

		ITypeDefOrRef GetTypeRef() => TypeNameParser.ParseReflection(ownerModule, typeFullName, null);

		protected override string Verify(string columnName) {
			if (columnName == nameof(TypeFullName)) {
				Type type;
				var error = LoadType(out type);
				if (!string.IsNullOrEmpty(error))
					return error;
				return string.Empty;
			}

			if (columnName == nameof(StringValue)) {
				object obj;
				return GetSerializedData(out obj);
			}

			return string.Empty;
		}

		public override bool HasError =>
			!string.IsNullOrEmpty(Verify(nameof(TypeFullName))) ||
			!string.IsNullOrEmpty(Verify(nameof(StringValue)));
	}
}
