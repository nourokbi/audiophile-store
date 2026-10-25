// One image with its desktop, tablet and mobile versions, the browser picks the right one
const Picture = ({ image, alt = '', className = '' }) => (
	<picture>
		<source media='(min-width: 1024px)' srcSet={image.desktop} />
		<source media='(min-width: 768px)' srcSet={image.tablet} />
		<img src={image.mobile} alt={alt} loading='lazy' className={className} />
	</picture>
);

export default Picture;
