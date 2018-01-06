$(() => {
  var nodeListSortable = sortable('.sidebar .node-list', {
    forcePlaceholderSize: true,
    connectWith: 'flow',
  });

  var flowSortable = sortable('.canvas .flow', {
    forcePlaceholderSize: true,
    connectWith: 'flow',
  });

  nodeListSortable[0].addEventListener('sortstart', (e) => {
    if (e.detail.startparent === document.querySelector('.canvas .flow')) {
      return;
    }

    var clone = e.detail.item.cloneNode(true);
    e.detail.startparent.appendChild(clone);
    sortable('.sidebar .node-list');
  });
});
