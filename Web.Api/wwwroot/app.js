// Náhled inzerátu: lokálně nakešovaný snímek -> hotlink na portál -> placeholder.
// Blazorové @onerror na <img> nefunguje (událost "error" nebublá a Blazor používá
// delegované listenery), proto obyčejný HTML atribut onerror.
window.listingThumbnailFallback = function (img) {
	const portalUrl = img.dataset.portalUrl;
	if (portalUrl) {
		// Portálovou URL zkoušíme jen jednou, jinak by se při mrtvém odkazu točilo dokola.
		img.dataset.portalUrl = '';
		img.src = portalUrl;
		return;
	}

	img.onerror = null;
	img.src = '/img/listing-placeholder.svg';
};
