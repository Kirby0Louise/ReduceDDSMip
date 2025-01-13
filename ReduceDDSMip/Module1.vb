Imports System.IO
Module Module1

    Public cmdArgs() As String

    Sub Main()
        'About
        Console.Clear()
        Console.WriteLine("ReduceDDSMip v1.0 by Kirby0Louise")
        Console.WriteLine("I'd appreciate it if you didn't project your armchairness onto me, k?")
        Console.WriteLine()
        Console.WriteLine("Use -h for help")

        cmdArgs = Environment.GetCommandLineArgs()

        If cmdArgs.Length = 1 Then
            'Ran without any args
        ElseIf cmdArgs.Length = 2 Then
            If cmdArgs(1) = "-h" Then
                'show help
                Console.WriteLine("Usage:")
                Console.WriteLine("ReduceDDSMip <DDS PATH>")
            Else
                reduceMip(cmdArgs(1))
            End If
        Else
            'too many args
            errorExit("ERROR - TOO MANY ARGUMENTS")
        End If
        Console.ReadLine()
    End Sub

    Sub errorExit(ByVal errorString As String)
        Console.WriteLine(errorString)
        Console.WriteLine("Press any key to escape")
        Console.ReadKey()
        Environment.Exit(0)
    End Sub

    Sub reduceMip(ByVal path As String)
        Console.WriteLine("--------------------------------------------------------------------------------")
        'File stream
        Dim fs1 As FileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Write)

        'reader
        Dim reader As BinaryReader = New BinaryReader(fs1)

        'copy DDS header
        Dim headerOutArray(&H7F) As Byte
        reader.Read(headerOutArray, 0, &H80)

        'check for mips > 1
        reader.BaseStream.Seek(&H1C, SeekOrigin.Begin)
        Dim mipCount As UInt32 = reader.ReadUInt32()

        If mipCount < 2 Then
            errorExit("ERROR - Does not contain any more mip levels.  Regenerate texture in graphics editor")
        End If
        Dim newMipCount As UInt32 = switchEndian(mipCount - 1)

        'get starting height and width
        Dim sHeight As UInt32
        Dim sWidth As UInt32
        reader.BaseStream.Seek(&HC, SeekOrigin.Begin)
        sHeight = reader.ReadUInt32()
        sWidth = reader.ReadUInt32()

        'check bit depth via fourCC
        Dim fourCCCode(3) As Byte
        reader.BaseStream.Seek(&H54, SeekOrigin.Begin)
        reader.Read(fourCCCode, 0, 4)

        Dim bitDepth As Integer

        If fourCCCode(3) = &H31 Then '0x31 = 1 in DXT1
            bitDepth = 4
        Else
            bitDepth = 8
        End If

        'Find number of bytes largest mip takes up

        Dim bytesForBigMip As UInt32 = sHeight * sWidth * bitDepth / 8

        'from ImHex DDS pattern
        reader.BaseStream.Seek(&H14, SeekOrigin.Begin)
        Dim pitchOrLinearSize As UInt32 = reader.ReadUInt32()

        'Find number of bytes to copy for rest of mips
        Dim newPOLS As UInt32 = pitchOrLinearSize - bytesForBigMip

        'Now copy them to output array

        reader.BaseStream.Seek(&H80 + bytesForBigMip, SeekOrigin.Begin)

        Dim s3tDataArray(newPOLS - 1) As Byte
        reader.Read(s3tDataArray, 0, newPOLS)

        'modify DDS header to be correct
        Dim newHeight As UInt32 = switchEndian(sHeight / 2)
        Dim newWidth As UInt32 = switchEndian(sWidth / 2)

        headerOutArray(&HC) = ((newHeight >> 24) And &HFFUI)
        headerOutArray(&HD) = ((newHeight >> 16) And &HFFUI)
        headerOutArray(&HE) = ((newHeight >> 8) And &HFFUI)
        headerOutArray(&HF) = ((newHeight) And &HFFUI)

        headerOutArray(&H10) = ((newWidth >> 24) And &HFFUI)
        headerOutArray(&H11) = ((newWidth >> 16) And &HFFUI)
        headerOutArray(&H12) = ((newWidth >> 8) And &HFFUI)
        headerOutArray(&H13) = ((newWidth) And &HFFUI)

        headerOutArray(&H14) = ((switchEndian(newPOLS) >> 24) And &HFFUI)
        headerOutArray(&H15) = ((switchEndian(newPOLS) >> 16) And &HFFUI)
        headerOutArray(&H16) = ((switchEndian(newPOLS) >> 8) And &HFFUI)
        headerOutArray(&H17) = ((switchEndian(newPOLS)) And &HFFUI)

        headerOutArray(&H1C) = ((newMipCount >> 24) And &HFFUI)
        headerOutArray(&H1D) = ((newMipCount >> 16) And &HFFUI)
        headerOutArray(&H1E) = ((newMipCount >> 8) And &HFFUI)
        headerOutArray(&H1F) = ((newMipCount) And &HFFUI)

        Dim finalData() As Byte = headerOutArray.Concat(s3tDataArray).ToArray()

        File.WriteAllBytes(Environment.CurrentDirectory + "\out\smaller.dds", finalData)

        Console.WriteLine("Reduced mip written to /out/smaller.dds")

    End Sub

    Private Function switchEndian(value As UInteger) As UInteger
        Dim result As UInteger = (value And &HFFUI) << 24 Or (value And &HFF00UI) << 8 Or (value And &HFF0000UI) >> 8 Or (value And &HFF000000UI) >> 24
        Return result
    End Function

End Module
