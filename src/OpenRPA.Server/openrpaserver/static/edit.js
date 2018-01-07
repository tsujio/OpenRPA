// TODO: use MVC library

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

  $('.canvas').on('click', '.node', (e) => {
    showNodePropertyPanel($(e.currentTarget));
  });

  function showNodePropertyPanel($node) {
    $('.node-property-panel').html('');

    var templateId;
    if ($node.hasClass('node-image-matching')) {
      templateId = '#tmplImageMatchingNodeProperty';
    } else {
      return;
    }

    var compiled = _.template($(templateId).html());
    var html = compiled({
      name: $node.text().trim(),
    });
    $('.node-property-panel').html(html);
  }

  $('.node-property-panel').on('click', '.image-matching-node-property .capture', () => {
    location.href = 'openrpa:capture/abcdefghijklmnopqrstuvwxyz';
  });
});
