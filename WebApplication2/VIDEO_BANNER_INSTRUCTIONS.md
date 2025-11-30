# Video Banner Setup Instructions

## 📹 Video Banner Added to Home Screen

A video banner section has been added to the home page that displays a muted, looping video.

### 📁 File Structure

1. **Video File Location**: Place your video file at:
   ```
   wwwroot/videos/home-banner.mp4
   ```

2. **Optional Poster Image**: For better loading experience, add a poster image at:
   ```
   wwwroot/images/video-banner-poster.jpg
   ```

### 🎬 Video Requirements

- **Duration**: 10 seconds maximum
- **File Size**: < 2MB for optimal page speed
- **Format**: MP4 (H.264 codec recommended for best compatibility)
- **Resolution**: 1920x1080 or 1280x720 (16:9 aspect ratio)
- **Content Suggestions**:
  - Gym workout scenes
  - Healthy lifestyle activities
  - Store packaging/products
  - Brand showcase

### 🎥 Video Compression Tips

To achieve < 2MB for a 10-second video:

1. **Use compression tools**:
   - HandBrake (free, open-source)
   - FFmpeg (command line)
   - Online compressors like CloudConvert

2. **Recommended settings**:
   - Resolution: 1280x720 (720p) instead of 1080p
   - Bitrate: 1-2 Mbps
   - Frame rate: 24-30 fps
   - Codec: H.264

3. **FFmpeg command example**:
   ```bash
   ffmpeg -i input.mp4 -vcodec h264 -acodec aac -b:v 1500k -b:a 128k -s 1280x720 -r 30 -t 10 output.mp4
   ```

### 📝 How to Update the Video

1. Replace the video file at `wwwroot/videos/home-banner.mp4`
2. (Optional) Update the poster image at `wwwroot/images/video-banner-poster.jpg`
3. Clear browser cache to see changes

### 🎨 Customization

The video banner can be customized in:
- **CSS**: `wwwroot/css/video-banner.css`
- **HTML**: `Areas/Customer/Views/Home/Index.cshtml` (look for "Video Banner Section")

### ✅ Current Features

- ✅ Muted autoplay
- ✅ Looping
- ✅ Mobile responsive
- ✅ Performance optimized (lazy loading)
- ✅ Graceful fallback if video fails to load
- ✅ Pauses when page is not visible (saves bandwidth)

### 🚀 Performance Notes

- Video only loads when in viewport (lazy loading)
- Video pauses when browser tab is not active
- Fallback gradient background if video fails
- Hardware acceleration enabled for smooth playback

### 📱 Mobile Considerations

- Video height adjusts for mobile screens
- Touch-optimized playback
- Reduced quality may be needed for very slow connections

---

**Note**: The video banner will show a gradient background until you add your video file. Once you place `home-banner.mp4` in the `wwwroot/videos/` folder, it will automatically display.

