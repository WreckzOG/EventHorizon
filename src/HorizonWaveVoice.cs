using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
namespace EventHorizon
{
    internal sealed class HorizonWaveAsset
    {
        internal byte[] Samples;
        internal int Rate;
        internal ushort Channels;
        internal static HorizonWaveAsset Load(string resource)
        {
            using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)){
                if(stream==null)throw new InvalidDataException("Missing embedded audio: "+resource);
                using(BinaryReader r=new BinaryReader(stream)){
                    if(r.ReadUInt32()!=0x46464952)throw new InvalidDataException("Expected RIFF");r.ReadUInt32();
                    if(r.ReadUInt32()!=0x45564157)throw new InvalidDataException("Expected WAVE");
                    var a=new HorizonWaveAsset();bool format=false;
                    while(stream.Position+8<=stream.Length){uint tag=r.ReadUInt32(),size=r.ReadUInt32();long next=stream.Position+size+(size&1);
                        if(next>stream.Length)throw new InvalidDataException("Truncated audio chunk");
                        if(tag==0x20746d66){if(size<16||r.ReadUInt16()!=1)throw new InvalidDataException("Expected PCM");a.Channels=r.ReadUInt16();a.Rate=r.ReadInt32();r.ReadUInt32();ushort align=r.ReadUInt16();if(r.ReadUInt16()!=16||a.Channels<1||a.Channels>2||align!=a.Channels*2||a.Rate<8000||a.Rate>192000)throw new InvalidDataException("Unsupported PCM format");format=true;}
                        else if(tag==0x61746164)a.Samples=r.ReadBytes(checked((int)size));
                        stream.Position=next;
                    }
                    if(!format||a.Samples==null||a.Samples.Length==0||a.Samples.Length%(a.Channels*2)!=0)throw new InvalidDataException("Invalid PCM frames");return a;
                }
            }
        }
    }
    internal sealed class HorizonWaveVoice:IDisposable
    {
        [StructLayout(LayoutKind.Sequential,Pack=2)]
        private struct Format {internal ushort Tag,Channels;internal uint Rate,BytesPerSecond;internal ushort Align,Bits,Extra;}
        [StructLayout(LayoutKind.Sequential)]
        private struct Header {internal IntPtr Data;internal uint Length,Recorded;internal UIntPtr User;internal uint Flags,Loops;internal IntPtr Next;internal UIntPtr Reserved;}
        [DllImport("winmm.dll")]private static extern uint waveOutOpen(out IntPtr handle,uint device,ref Format format,IntPtr callback,IntPtr instance,uint flags);
        [DllImport("winmm.dll")]private static extern uint waveOutPrepareHeader(IntPtr handle,IntPtr header,uint size);
        [DllImport("winmm.dll")]private static extern uint waveOutWrite(IntPtr handle,IntPtr header,uint size);
        [DllImport("winmm.dll")]private static extern uint waveOutPause(IntPtr handle);
        [DllImport("winmm.dll")]private static extern uint waveOutRestart(IntPtr handle);
        [DllImport("winmm.dll")]private static extern uint waveOutReset(IntPtr handle);
        [DllImport("winmm.dll")]private static extern uint waveOutUnprepareHeader(IntPtr handle,IntPtr header,uint size);
        [DllImport("winmm.dll")]private static extern uint waveOutClose(IntPtr handle);
        private IntPtr device,header,data;
        private bool prepared,paused;
        private static readonly uint HeaderSize=(uint)Marshal.SizeOf(typeof(Header));
        internal HorizonWaveVoice(HorizonWaveAsset clip,bool loop,float elapsed,float gain)
        {
            try {
                int align=clip.Channels*2,frames=clip.Samples.Length/align;
                long frame=(long)(Math.Max(0,elapsed)*clip.Rate);
                int offset=(int)(loop?frame%frames:Math.Min(frame,frames-1))*align;
                byte[] pcm=new byte[loop?clip.Samples.Length:clip.Samples.Length-offset];
                int tail=clip.Samples.Length-offset;Buffer.BlockCopy(clip.Samples,offset,pcm,0,tail);
                if(loop&&offset>0)Buffer.BlockCopy(clip.Samples,0,pcm,tail,offset);
                // Scale only this voice, never the audio device or other application sounds.
                for(int i=0;i<pcm.Length;i+=2){short sample=(short)(pcm[i]|pcm[i+1]<<8);short scaled=(short)(sample*Math.Max(0,Math.Min(1,gain)));pcm[i]=(byte)scaled;pcm[i+1]=(byte)(scaled>>8);}
                Format format=new Format{Tag=1,Channels=clip.Channels,Rate=(uint)clip.Rate,BytesPerSecond=(uint)(clip.Rate*align),Align=(ushort)align,Bits=16};
                Check(waveOutOpen(out device,uint.MaxValue,ref format,IntPtr.Zero,IntPtr.Zero,0),"open");
                data=Marshal.AllocHGlobal(pcm.Length);Marshal.Copy(pcm,0,data,pcm.Length);
                header=Marshal.AllocHGlobal((int)HeaderSize);
                Marshal.StructureToPtr(new Header{Data=data,Length=(uint)pcm.Length,Flags=0,Loops=loop?uint.MaxValue:0},header,false);
                Check(waveOutPrepareHeader(device,header,HeaderSize),"prepare");prepared=true;
                if(loop){Header ready=(Header)Marshal.PtrToStructure(header,typeof(Header));ready.Flags|=12u;Marshal.StructureToPtr(ready,header,false);}
                Check(waveOutWrite(device,header,HeaderSize),"play");
            }catch{Dispose();throw;}
        }
        internal void Pause(bool value){if(device==IntPtr.Zero||paused==value)return;Check(value?waveOutPause(device):waveOutRestart(device),"pause/resume");paused=value;}
        private static void Check(uint result,string operation){if(result!=0)throw new InvalidOperationException("Windows audio "+operation+" failed ("+result+")");}
        public void Dispose()
        {
            if(device!=IntPtr.Zero){waveOutReset(device);if(prepared)waveOutUnprepareHeader(device,header,HeaderSize);waveOutClose(device);device=IntPtr.Zero;prepared=false;}
            if(header!=IntPtr.Zero){Marshal.FreeHGlobal(header);header=IntPtr.Zero;}
            if(data!=IntPtr.Zero){Marshal.FreeHGlobal(data);data=IntPtr.Zero;}
        }
    }
}
