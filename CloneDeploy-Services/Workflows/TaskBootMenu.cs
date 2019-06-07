﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CloneDeploy_ApiCalls;
using CloneDeploy_Common;
using CloneDeploy_Entities;
using CloneDeploy_Entities.DTOs;

namespace CloneDeploy_Services.Workflows
{
    public class TaskBootMenu
    {
        private readonly ClusterGroupServices _clusterGroupServices;
        private readonly ComputerEntity _computer;
        private readonly ImageProfileEntity _imageProfile;
        private readonly SecondaryServerServices _secondaryServerServices;

        public TaskBootMenu(ComputerEntity computer, ImageProfileEntity imageProfile)
        {
            _computer = computer;
            _imageProfile = imageProfile;
            _clusterGroupServices = new ClusterGroupServices();
            _secondaryServerServices = new SecondaryServerServices();
        }

        public bool CreatePxeBootFiles()
        {
            var pxeComputerMac = StringManipulationServices.MacToPxeMac(_computer.Mac);
            var webPath = SettingServices.GetSettingValue(SettingStrings.WebPath) + "api/ClientImaging/";
            var globalComputerArgs = SettingServices.GetSettingValue(SettingStrings.GlobalComputerArgs);
            var userToken = SettingServices.GetSettingValue(SettingStrings.WebTaskRequiresLogin) == "No"
                ? SettingServices.GetSettingValue(SettingStrings.UniversalToken)
                : "";
            const string newLineChar = "\n";

            if (_computer.AlternateServerIpId != -1)
            {
                var altServer = new AlternateServerIpServices().GetAlternateServerIp(_computer.AlternateServerIpId);
                if (altServer != null)
                {
                    webPath = altServer.ApiUrl + "api/ClientImaging/";
                }
            }

            var replacedPath = webPath;
            if (SettingServices.GetSettingValue(SettingStrings.IpxeSSL).Equals("1"))
            {
                replacedPath = replacedPath.ToLower().Replace("http", "https");
            }
            else
            {
                replacedPath = replacedPath.ToLower().Replace("https", "http");
            }

            var ipxe = new StringBuilder();
            ipxe.Append("#!ipxe" + newLineChar);
            ipxe.Append("kernel " + replacedPath + "IpxeBoot?filename=" + _imageProfile.Kernel +
                        "&type=kernel" + " initrd=" + _imageProfile.BootImage +
                        " root=/dev/ram0 rw ramdisk_size=156000" +
                        " consoleblank=0" + " web=" + webPath + " USER_TOKEN=" + userToken + " " + globalComputerArgs +
                        " " + _imageProfile.KernelArguments + newLineChar);
            ipxe.Append("imgfetch --name " + _imageProfile.BootImage + " " + replacedPath +
                        "IpxeBoot?filename=" + _imageProfile.BootImage + "&type=bootimage" + newLineChar);
            ipxe.Append("boot" + newLineChar);

            var sysLinux = new StringBuilder();
            sysLinux.Append("DEFAULT clonedeploy" + newLineChar);
            sysLinux.Append("LABEL clonedeploy" + newLineChar);
            sysLinux.Append("KERNEL kernels" + Path.DirectorySeparatorChar + _imageProfile.Kernel + newLineChar);
            sysLinux.Append("APPEND initrd=images" + Path.DirectorySeparatorChar + _imageProfile.BootImage +
                            " root=/dev/ram0 rw ramdisk_size=156000" +
                            " consoleblank=0" + " web=" + webPath + " USER_TOKEN=" + userToken + " " +
                            globalComputerArgs +
                            " " + _imageProfile.KernelArguments + newLineChar);

            var grub = new StringBuilder();
            grub.Append("set default=0" + newLineChar);
            grub.Append("set timeout=0" + newLineChar);
            grub.Append("menuentry CloneDeploy --unrestricted {" + newLineChar);
            grub.Append("echo Please Wait While The Boot Image Is Transferred.  This May Take A Few Minutes." +
                        newLineChar);
            grub.Append("linux /kernels/" + _imageProfile.Kernel +
                        " root=/dev/ram0 rw ramdisk_size=156000" + " consoleblank=0" + " web=" + webPath +
                        " USER_TOKEN=" +
                        userToken + " " +
                        globalComputerArgs + " " + _imageProfile.KernelArguments + newLineChar);
            grub.Append("initrd /images/" + _imageProfile.BootImage + newLineChar);
            grub.Append("}" + newLineChar);

            var list = new List<Tuple<string, string, string>>
            {
                Tuple.Create("bios", "", sysLinux.ToString()),
                Tuple.Create("bios", ".ipxe", ipxe.ToString()),
                Tuple.Create("efi32", "", sysLinux.ToString()),
                Tuple.Create("efi32", ".ipxe", ipxe.ToString()),
                Tuple.Create("efi64", "", sysLinux.ToString()),
                Tuple.Create("efi64", ".ipxe", ipxe.ToString()),
                Tuple.Create("efi64", ".cfg", grub.ToString())
            };

            //In proxy mode all boot files are created regardless of the pxe mode, this way computers can be customized
            //to use a specific boot file without affecting all others, using the proxydhcp reservations file.
            if (SettingServices.GetSettingValue(SettingStrings.ProxyDhcp) == "Yes")
            {
                if (SettingServices.ServerIsNotClustered)
                {
                    foreach (var bootMenu in list)
                    {
                        var path = SettingServices.GetSettingValue(SettingStrings.TftpPath) + "proxy" +
                                   Path.DirectorySeparatorChar + bootMenu.Item1 +
                                   Path.DirectorySeparatorChar + "pxelinux.cfg" + Path.DirectorySeparatorChar +
                                   pxeComputerMac +
                                   bootMenu.Item2;

                        if (!new FileOpsServices().WritePath(path, bootMenu.Item3))
                            return false;
                    }
                }
                else
                {
                    var clusterGroup = new ComputerServices().GetClusterGroup(_computer.Id);
                    var tftpServers = _clusterGroupServices.GetClusterTftpServers(clusterGroup.Id);   // patch by Diederick Niehorster! See comment below. Ask Diederick
                    if (tftpServers.Count == 0)
                    {
                        // no tftp server found for this cluster. I have seen that happen when using a
                        // fake cluster where both members are different NICs on the same computer. That
                        // means no secondary server can be defined for the cluster, which messes up
                        // defining tftp role for for the cluster group (foreign key constraint failure
                        // because there is no secondary server). Then we get count=0 here. Fall back to
                        // seeing if the server we're on has tftp role defined in its cluster server role
                        // and if so, use it
                        if (SettingServices.GetSettingValue(SettingStrings.TftpServerRole).Equals("1"))
                        {
                            var fakeServer = new ClusterGroupServerEntity();
                            fakeServer.ServerId = -1;    // only thing that needs to be set
                            tftpServers.Add(fakeServer);
                        }
                    }
                    foreach (var tftpServer in tftpServers)
                    {
                        foreach (var bootMenu in list)
                        {
                            if (tftpServer.ServerId == -1)
                            {
                                var path = SettingServices.GetSettingValue(SettingStrings.TftpPath) + "proxy" +
                                           Path.DirectorySeparatorChar + bootMenu.Item1 +
                                           Path.DirectorySeparatorChar + "pxelinux.cfg" + Path.DirectorySeparatorChar +
                                           pxeComputerMac +
                                           bootMenu.Item2;

                                if (!new FileOpsServices().WritePath(path, bootMenu.Item3))
                                    return false;
                            }
                            else
                            {
                                var secondaryServer = _secondaryServerServices.GetSecondaryServer(tftpServer.ServerId);
                                var tftpPath =
                                    new APICall(_secondaryServerServices.GetToken(secondaryServer.Name))
                                        .SettingApi.GetSetting("Tftp Path").Value;

                                var path = tftpPath + "proxy" + Path.DirectorySeparatorChar + bootMenu.Item1 +
                                           Path.DirectorySeparatorChar + "pxelinux.cfg" + Path.DirectorySeparatorChar +
                                           pxeComputerMac +
                                           bootMenu.Item2;

                                if (
                                    !new APICall(_secondaryServerServices.GetToken(secondaryServer.Name))
                                        .ServiceAccountApi.WriteTftpFile(new TftpFileDTO
                                        {
                                            Path = path,
                                            Contents = bootMenu.Item3
                                        }))
                                    return false;
                            }
                        }
                    }
                }
            }
            //When not using proxy dhcp, only one boot file is created
            else
            {
                var mode = SettingServices.GetSettingValue(SettingStrings.PxeMode);
                var path = "";
                if (SettingServices.ServerIsNotClustered)
                {
                    path = SettingServices.GetSettingValue(SettingStrings.TftpPath) + "pxelinux.cfg" +
                           Path.DirectorySeparatorChar + pxeComputerMac;

                    string fileContents = null;
                    if (mode == "pxelinux" || mode == "syslinux_32_efi" || mode == "syslinux_64_efi")
                    {
                        fileContents = sysLinux.ToString();
                    }

                    else if (mode.Contains("ipxe"))
                    {
                        path += ".ipxe";
                        fileContents = ipxe.ToString();
                    }
                    else if (mode.Contains("grub"))
                    {
                        path += ".cfg";
                        fileContents = grub.ToString();
                    }

                    if (!new FileOpsServices().WritePath(path, fileContents))
                        return false;
                }
                else
                {
                    var clusterGroup = new ComputerServices().GetClusterGroup(_computer.Id);
                    var secondaryServer = new SecondaryServerEntity();
                    var tftpServers = _clusterGroupServices.GetClusterTftpServers(clusterGroup.Id);   // patch by Diederick Niehorster! See comment below. Ask Diederick
                    if (tftpServers.Count==0)
                    {
                        // no tftp server found for this cluster. I have seen that happen when using a
                        // fake cluster where both members are different NICs on the same computer. That
                        // means no secondary server can be defined for the cluster, which messes up
                        // defining tftp role for for the cluster group (foreign key constraint failure
                        // because there is no secondary server). Then we get count=0 here. Fall back to
                        // seeing if the server we're on has tftp role defined in its cluster server role
                        // and if so, use it
                        if (SettingServices.GetSettingValue(SettingStrings.TftpServerRole).Equals("1"))
                        {
                            var fakeServer = new ClusterGroupServerEntity();
                            fakeServer.ServerId = -1;    // only thing that needs to be set
                            tftpServers.Add(fakeServer);
                        }
                    }
                    foreach (var tftpServer in tftpServers)
                    {
                        if (tftpServer.ServerId == -1)
                        {
                            path = SettingServices.GetSettingValue(SettingStrings.TftpPath) + "pxelinux.cfg" +
                                   Path.DirectorySeparatorChar + pxeComputerMac;
                        }
                        else
                        {
                            secondaryServer = _secondaryServerServices.GetSecondaryServer(tftpServer.ServerId);
                            var tftpPath =
                                new APICall(_secondaryServerServices.GetToken(secondaryServer.Name)).SettingApi
                                    .GetSetting("Tftp Path").Value;

                            path = tftpPath + "pxelinux.cfg" + Path.DirectorySeparatorChar + pxeComputerMac;
                        }

                        string fileContents = null;
                        if (mode == "pxelinux" || mode == "syslinux_32_efi" || mode == "syslinux_64_efi")
                        {
                            fileContents = sysLinux.ToString();
                        }

                        else if (mode.Contains("ipxe"))
                        {
                            path += ".ipxe";
                            fileContents = ipxe.ToString();
                        }
                        else if (mode.Contains("grub"))
                        {
                            path += ".cfg";
                            fileContents = grub.ToString();
                        }

                        if (tftpServer.ServerId == -1)
                        {
                            if (!new FileOpsServices().WritePath(path, fileContents))
                                return false;
                        }
                        else
                        {
                            if (
                                !new APICall(_secondaryServerServices.GetToken(secondaryServer.Name))
                                    .ServiceAccountApi.WriteTftpFile(new TftpFileDTO
                                    {
                                        Path = path,
                                        Contents = fileContents
                                    }))
                                return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}