export interface GalleryProps {
  images: string[];
  className?: string;
}

export const Gallery = ({ images, className = '' }: GalleryProps) => {
  if (images.length === 0) return null;

  return (
    <div className={`grid grid-cols-1 md:grid-cols-3 gap-4 ${className}`}>
      {images.map((image, index) => (
        <img
          key={index}
          src={image}
          alt={`Gallery image ${index + 1}`}
          className="w-full h-64 object-cover rounded-lg"
        />
      ))}
    </div>
  );
};

