﻿//  Copyright 2021 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace NtApiDotNet.Win32.Net
{
    /// <summary>
    /// Class to represent a TCP listener with process ID.
    /// </summary>
    public class TcpListenerInformation : TcpConnectionInformation
    {
        /// <summary>Gets the local endpoint of a Transmission Control Protocol (TCP) connection.</summary>
        /// <returns>An <see cref="T:System.Net.IPEndPoint" /> instance that contains the IP address and port on the local computer.</returns>
        public override IPEndPoint LocalEndPoint { get; }

        /// <summary>Gets the remote endpoint of a Transmission Control Protocol (TCP) connection.</summary>
        /// <returns>An <see cref="T:System.Net.IPEndPoint" /> instance that contains the IP address and port on the remote computer.</returns>
        public override IPEndPoint RemoteEndPoint { get; }

        /// <summary>Gets the state of this Transmission Control Protocol (TCP) connection.</summary>
        /// <returns>One of the <see cref="T:System.Net.NetworkInformation.TcpState" /> enumeration values.</returns>
        public override TcpState State { get; }

        /// <summary>
        /// Gets the process ID of the listener on the local system.
        /// </summary>
        public int ProcessId { get; }

        /// <summary>
        /// Gets the time the socket was created.
        /// </summary>
        public DateTime CreateTime { get; }

        /// <summary>
        /// Gets the owner of the module. This could be an executable path or a service name.
        /// </summary>
        public string OwnerModule { get; }

        private static int ConvertPort(int port)
        {
            return ((port & 0xFF) << 8) | ((port >> 8) & 0xFF);
        }

        private static string GetOwnerModule<T>(GetOwnerModuleDelegate<T> func, T entry, int process_id)
        {
            using (var buffer = new SafeStructureInOutBuffer<TCPIP_OWNER_MODULE_BASIC_INFO>(64 * 1024, true))
            {
                int size = buffer.Length;
                Win32Error error = func(entry, TCPIP_OWNER_MODULE_INFO_CLASS.TCPIP_OWNER_MODULE_INFO_BASIC, buffer, ref size);
                string ret;
                if (error == Win32Error.SUCCESS)
                {
                    ret = Marshal.PtrToStringUni(buffer.Result.pModulePath);
                }
                else
                {
                    ret = NtSystemInfo.GetProcessIdImagePath(process_id, false).GetResultOrDefault(string.Empty);
                }
                return ret;
            }
        }

        internal TcpListenerInformation(MIB_TCPROW_OWNER_PID entry)
        {
            LocalEndPoint = new IPEndPoint(entry.dwLocalAddr, ConvertPort(entry.dwLocalPort));
            RemoteEndPoint = new IPEndPoint(entry.dwRemoteAddr, ConvertPort(entry.dwRemotePort));
            State = (TcpState)entry.dwState;
            ProcessId = entry.dwOwningPid;
            OwnerModule = NtSystemInfo.GetProcessIdImagePath(ProcessId, false).GetResultOrDefault(string.Empty);
        }

        internal TcpListenerInformation(MIB_TCPROW_OWNER_MODULE entry)
        {
            LocalEndPoint = new IPEndPoint(entry.dwLocalAddr, ConvertPort(entry.dwLocalPort));
            RemoteEndPoint = new IPEndPoint(entry.dwRemoteAddr, ConvertPort(entry.dwRemotePort));
            State = (TcpState)entry.dwState;
            ProcessId = entry.dwOwningPid;
            CreateTime = entry.liCreateTimestamp.ToDateTime();
            OwnerModule = GetOwnerModule(Win32NetworkNativeMethods.GetOwnerModuleFromTcpEntry, entry, ProcessId);
        }

        internal TcpListenerInformation(MIB_TCP6ROW_OWNER_PID entry)
        {
            LocalEndPoint = new IPEndPoint(new IPAddress(entry.ucLocalAddr.ToArray(), entry.dwLocalScopeId),
                ConvertPort(entry.dwLocalPort));
            RemoteEndPoint = new IPEndPoint(new IPAddress(entry.ucRemoteAddr.ToArray(), entry.dwRemoteScopeId),
                ConvertPort(entry.dwRemotePort));
            State = (TcpState)entry.dwState;
            ProcessId = entry.dwOwningPid;
        }

        internal TcpListenerInformation(MIB_TCP6ROW_OWNER_MODULE entry)
        {
            LocalEndPoint = new IPEndPoint(new IPAddress(entry.ucLocalAddr.ToArray(), entry.dwLocalScopeId),
                ConvertPort(entry.dwLocalPort));
            RemoteEndPoint = new IPEndPoint(new IPAddress(entry.ucRemoteAddr.ToArray(), entry.dwRemoteScopeId),
                ConvertPort(entry.dwRemotePort));
            State = (TcpState)entry.dwState;
            ProcessId = entry.dwOwningPid;
            CreateTime = entry.liCreateTimestamp.ToDateTime();
            OwnerModule = GetOwnerModule(Win32NetworkNativeMethods.GetOwnerModuleFromTcp6Entry, entry, ProcessId);
        }
    }
}
