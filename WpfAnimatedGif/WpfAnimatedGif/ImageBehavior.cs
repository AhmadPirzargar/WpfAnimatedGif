﻿using System;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using WpfAnimatedGif.Decoding;

namespace WpfAnimatedGif
{
    /// <summary>
    /// Provides attached properties that display animated GIFs in a standard Image control.
    /// </summary>
    public static class ImageBehavior
    {
        /// <summary>
        /// Gets the value of the <c>AnimatedSource</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The currently displayed animated image.</returns>
        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static ImageSource GetAnimatedSource(Image obj)
        {
            return (ImageSource)obj.GetValue(AnimatedSourceProperty);
        }

        /// <summary>
        /// Sets the value of the <c>AnimatedSource</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element on which to set the property value.</param>
        /// <param name="value">The animated image to display.</param>
        public static void SetAnimatedSource(Image obj, ImageSource value)
        {
            obj.SetValue(AnimatedSourceProperty, value);
        }

        /// <summary>
        /// Identifies the <c>AnimatedSource</c> attached property.
        /// </summary>
        public static readonly DependencyProperty AnimatedSourceProperty =
            DependencyProperty.RegisterAttached(
              "AnimatedSource",
              typeof(ImageSource),
              typeof(ImageBehavior),
              new UIPropertyMetadata(
                null,
                AnimatedSourceChanged));

        /// <summary>
        /// Gets the value of the <c>RepeatBehavior</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The repeat behavior of the animated image.</returns>
        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static RepeatBehavior GetRepeatBehavior(Image obj)
        {
            return (RepeatBehavior)obj.GetValue(RepeatBehaviorProperty);
        }

        /// <summary>
        /// Sets the value of the <c>RepeatBehavior</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element on which to set the property value.</param>
        /// <param name="value">The repeat behavior of the animated image.</param>
        public static void SetRepeatBehavior(Image obj, RepeatBehavior value)
        {
            obj.SetValue(RepeatBehaviorProperty, value);
        }

        /// <summary>
        /// Identifies the <c>RepeatBehavior</c> attached property.
        /// </summary>
        public static readonly DependencyProperty RepeatBehaviorProperty =
            DependencyProperty.RegisterAttached(
              "RepeatBehavior",
              typeof(RepeatBehavior),
              typeof(ImageBehavior),
              new UIPropertyMetadata(
                  default(RepeatBehavior),
                  RepeatBehaviorChanged));

        /// <summary>
        /// Gets the value of the <c>AnimateInDesignMode</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>true if GIF animations are shown in design mode; false otherwise.</returns>
        public static bool GetAnimateInDesignMode(DependencyObject obj)
        {
            return (bool)obj.GetValue(AnimateInDesignModeProperty);
        }

        /// <summary>
        /// Sets the value of the <c>AnimateInDesignMode</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element on which to set the property value.</param>
        /// <param name="value">true to show GIF animations in design mode; false otherwise.</param>
        public static void SetAnimateInDesignMode(DependencyObject obj, bool value)
        {
            obj.SetValue(AnimateInDesignModeProperty, value);
        }

        /// <summary>
        /// Identifies the <c>AnimateInDesignMode</c> attached property.
        /// </summary>
        public static readonly DependencyProperty AnimateInDesignModeProperty =
            DependencyProperty.RegisterAttached(
                "AnimateInDesignMode",
                typeof(bool),
                typeof(ImageBehavior),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.Inherits,
                    AnimateInDesignModeChanged));

        /// <summary>
        /// Identifies the <c>AnimationCompleted</c> attached event.
        /// </summary>
        public static readonly RoutedEvent AnimationCompletedEvent =
            EventManager.RegisterRoutedEvent(
                "AnimationCompleted",
                RoutingStrategy.Bubble,
                typeof (RoutedEventHandler),
                typeof (ImageBehavior));

        /// <summary>
        /// Adds a handler for the AnimationCompleted attached event.
        /// </summary>
        /// <param name="d">The UIElement that listens to this event.</param>
        /// <param name="handler">The event handler to be added.</param>
        public static void AddAnimationCompletedHandler(DependencyObject d, RoutedEventHandler handler)
        {
            var element = d as UIElement;
            if (element == null)
                return;
            element.AddHandler(AnimationCompletedEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the AnimationCompleted attached event.
        /// </summary>
        /// <param name="d">The UIElement that listens to this event.</param>
        /// <param name="handler">The event handler to be removed.</param>
        public static void RemoveAnimationCompletedHandler(DependencyObject d, RoutedEventHandler handler)
        {
            var element = d as UIElement;
            if (element == null)
                return;
            element.RemoveHandler(AnimationCompletedEvent, handler);
        }

        private static void AnimatedSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Image imageControl = o as Image;
            if (imageControl == null)
                return;

            var oldValue = e.OldValue as ImageSource;
            var newValue = e.NewValue as ImageSource;
            if (oldValue != null)
            {
                imageControl.BeginAnimation(Image.SourceProperty, null);
            }
            if (newValue != null)
            {
                imageControl.DoWhenLoaded(InitAnimationOrImage);
            }
        }

        private static void RepeatBehaviorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Image imageControl = o as Image;
            if (imageControl == null)
                return;

            ImageSource source = GetAnimatedSource(imageControl);
            if (source != null && imageControl.IsLoaded)
                InitAnimationOrImage(imageControl);
        }

        private static void AnimateInDesignModeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Image imageControl = o as Image;
            if (imageControl == null)
                return;

            bool newValue = (bool) e.NewValue;

            ImageSource source = GetAnimatedSource(imageControl);
            if (source != null && imageControl.IsLoaded)
            {
                if (newValue)
                    InitAnimationOrImage(imageControl);
                else
                    imageControl.BeginAnimation(Image.SourceProperty, null);
            }
        }

        private static void InitAnimationOrImage(Image imageControl)
        {
            BitmapSource source = GetAnimatedSource(imageControl) as BitmapSource;
            bool isInDesignMode = DesignerProperties.GetIsInDesignMode(imageControl);
            bool animateInDesignMode = GetAnimateInDesignMode(imageControl);
            bool shouldAnimate = !isInDesignMode || animateInDesignMode;
            if (source != null && shouldAnimate)
            {
                GifFile gifFile;
                var decoder = GetDecoder(source, out gifFile) as GifBitmapDecoder;
                if (decoder != null && decoder.Frames.Count > 1)
                {
                    int index = 0;
                    var animation = new ObjectAnimationUsingKeyFrames();
                    var totalDuration = TimeSpan.Zero;
                    BitmapSource prevFrame = null;
                    FrameInfo prevInfo = null;                    
                    foreach (var rawFrame in decoder.Frames)
                    {
                        GifFrame gifFrame = null;
                        if (gifFile != null && index < gifFile.Frames.Count)
                            gifFrame = gifFile.Frames[index];
                        var info = GetFrameInfo(rawFrame, gifFrame);
                        var frame = MakeFrame(
                            source,
                            rawFrame, info,
                            prevFrame, prevInfo);

                        var keyFrame = new DiscreteObjectKeyFrame(frame, totalDuration);
                        animation.KeyFrames.Add(keyFrame);
                        
                        totalDuration += info.Delay;
                        prevFrame = frame;
                        prevInfo = info;
                        index++;
                    }
                    animation.Duration = totalDuration;
                    
                    var repeatBehavior = GetRepeatBehavior(imageControl);
                    if (repeatBehavior == default(RepeatBehavior))
                    {
                        // Unspecified repeat behavior: use repeatCount from GIF metadata
                        ushort repeatCount = gifFile != null ? gifFile.RepeatCount : (ushort)0;
                        repeatBehavior =
                            repeatCount == 0
                                ? RepeatBehavior.Forever
                                : new RepeatBehavior(repeatCount);
                    }
                    animation.RepeatBehavior = repeatBehavior;

                    if (animation.KeyFrames.Count > 0)
                        imageControl.Source = (ImageSource)animation.KeyFrames[0].Value;
                    else
                        imageControl.Source = decoder.Frames[0];
                    animation.Completed += delegate
                                           {
                                               imageControl.RaiseEvent(
                                                   new RoutedEventArgs(AnimationCompletedEvent, imageControl));
                                           };
                    imageControl.BeginAnimation(Image.SourceProperty, animation);
                    return;
                }
            }
            imageControl.Source = source;
        }

        private static BitmapDecoder GetDecoder(BitmapSource image, out GifFile gifFile)
        {
            gifFile = null;
            BitmapDecoder decoder = null;
            Stream stream = null;
            Uri uri = null;
            BitmapCreateOptions createOptions = BitmapCreateOptions.None;
            
            var bmp = image as BitmapImage;
            if (bmp != null)
            {
                createOptions = bmp.CreateOptions;
                if (bmp.StreamSource != null)
                {
                    stream = bmp.StreamSource;
                }
                else if (bmp.UriSource != null)
                {
                    uri = bmp.UriSource;
                    if (bmp.BaseUri != null && !uri.IsAbsoluteUri)
                        uri = new Uri(bmp.BaseUri, uri);
                }
            }
            else
            {
                BitmapFrame frame = image as BitmapFrame;
                if (frame != null)
                {
                    decoder = frame.Decoder;
                    Uri.TryCreate(frame.BaseUri, frame.ToString(), out uri);
                }
            }

            if (stream != null)
            {
                stream.Position = 0;
                decoder = BitmapDecoder.Create(stream, createOptions, BitmapCacheOption.OnLoad);
                stream.Position = 0;
                gifFile = GifDecoder.DecodeGif(stream, true);
            }
            else if (uri != null)
            {
                decoder = BitmapDecoder.Create(uri, createOptions, BitmapCacheOption.OnLoad);
                gifFile = DecodeGifFile(uri);
            }
            return decoder;
        }

        private static GifFile DecodeGifFile(Uri uri)
        {
            Stream stream = null;
            if (uri.Scheme == PackUriHelper.UriSchemePack)
            {
                var sri = Application.GetResourceStream(uri);
                if (sri != null)
                    stream = sri.Stream;
            }
            else
            {
                WebClient wc = new WebClient();
                stream = wc.OpenRead(uri);
            }
            if (stream != null)
            {
                using (stream)
                {
                    return GifDecoder.DecodeGif(stream, true);
                }
            }
            return null;
        }

        private static BitmapSource MakeFrame(
            BitmapSource fullImage,
            BitmapSource rawFrame, FrameInfo frameInfo,
            BitmapSource previousFrame, FrameInfo previousFrameInfo)
        {
            DrawingVisual visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                if (previousFrameInfo != null && previousFrame != null &&
                    previousFrameInfo.DisposalMethod == FrameDisposalMethod.Combine)
                {
                    var fullRect = new Rect(0, 0, fullImage.PixelWidth, fullImage.PixelHeight);
                    context.DrawImage(previousFrame, fullRect);
                }

                context.DrawImage(rawFrame, frameInfo.Rect);
            }
            var bitmap = new RenderTargetBitmap(
                fullImage.PixelWidth, fullImage.PixelHeight,
                fullImage.DpiX, fullImage.DpiY,
                PixelFormats.Pbgra32);
            bitmap.Render(visual);
            return bitmap;
        }

        private class FrameInfo
        {
            public TimeSpan Delay { get; set; }
            public FrameDisposalMethod DisposalMethod { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public double Left { get; set; }
            public double Top { get; set; }

            public Rect Rect
            {
                get { return new Rect(Left, Top, Width, Height); }
            }
        }

        private enum FrameDisposalMethod
        {
            Replace = 0,
            Combine = 1,
            // ReSharper disable UnusedMember.Local
            RestoreBackground = 2,
            RestorePrevious = 3
            // ReSharper restore UnusedMember.Local
        }

        private static FrameInfo GetFrameInfo(BitmapFrame frame, GifFrame gifFrame)
        {
            var frameInfo = new FrameInfo
            {
                Delay = TimeSpan.FromMilliseconds(100),
                DisposalMethod = FrameDisposalMethod.Replace,
                Width = frame.PixelWidth,
                Height = frame.PixelHeight,
                Left = 0,
                Top = 0
            };

            try
            {
                if (gifFrame != null)
                {
                    var gce = gifFrame.Extensions.OfType<GifGraphicControlExtension>().FirstOrDefault();
                    if (gce != null)
                    {
                        frameInfo.DisposalMethod = (FrameDisposalMethod)gce.DisposalMethod;
                        if (gce.Delay > 0)
                            frameInfo.Delay = TimeSpan.FromMilliseconds(gce.Delay);
                    }
                    if (gifFrame.Descriptor.Left > 0)
                        frameInfo.Left = gifFrame.Descriptor.Left;
                    if (gifFrame.Descriptor.Top > 0)
                        frameInfo.Top = gifFrame.Descriptor.Top;
                    if (gifFrame.Descriptor.Width > 0)
                        frameInfo.Width = gifFrame.Descriptor.Width;
                    if (gifFrame.Descriptor.Height > 0)
                        frameInfo.Height = gifFrame.Descriptor.Height;
                }
            }
            catch (NotSupportedException)
            {
            }

            return frameInfo;
        }

        private static void DoWhenLoaded<T>(this T element, Action<T> action)
            where T : FrameworkElement
        {
            if (element.IsLoaded)
            {
                action(element);
            }
            else
            {
                RoutedEventHandler handler = null;
                handler = (sender, e) =>
                {
                    element.Loaded -= handler;
                    action(element);
                };
                element.Loaded += handler;
            }
        }
    }
}
