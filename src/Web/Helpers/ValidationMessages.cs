﻿// Copyright (c) 2024 Sergio Hernandez. All rights reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License").
//  You may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//

using System.Globalization;
using System.Resources;

namespace Security.Web.Helpers;

public static class ValidationMessages
{
    private static readonly ResourceManager ResourceManager = new("Web.Resources.ValidationMessages", typeof(ValidationMessages).Assembly);

    public static string EmailRequired => ResourceManager.GetString("EmailRequired", CultureInfo.CurrentCulture)!;
    public static string InvalidEmailAddress => ResourceManager.GetString("InvalidEmail", CultureInfo.CurrentCulture)!;
    public static string PasswordRequired => ResourceManager.GetString("PasswordRequired", CultureInfo.CurrentCulture)!;
    public static string PasswordLength => ResourceManager.GetString("InvalidPassword", CultureInfo.CurrentCulture)!;
}