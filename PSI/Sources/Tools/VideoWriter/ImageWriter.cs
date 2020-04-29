using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Video.FFMPEG;

namespace VideoWriter
{
    class ImageWriter : SimpleConsumer<Shared<EncodedImage>>
    {
        private VideoFileWriter writer;
        private string path;

        public ImageWriter(Pipeline p, string path)
            : base(p)
        {
            this.writer = new VideoFileWriter();
            writer.Open()
            this.path = path;

        }

        public override void Receive(Message<Shared<EncodedImage>> message)
        {
            var img = message.Data
        }
    }
}
