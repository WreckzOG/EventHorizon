from pathlib import Path
import subprocess
import imageio_ffmpeg
root=Path(__file__).resolve().parent
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-ss','1','-t','12','-i',str(root/'media/event-horizon-demo.mp4'),'-filter_complex','fps=10,scale=640:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=96:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=3:diff_mode=rectangle','-loop','0',str(root/'media/event-horizon-preview.gif')],check=True)
print('Gameplay preview created.')
