// TODO: use MVC library

var capture, rect, title;

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

  $('.node-property-panel').on('click', '.image-matching-node-property .capture', (e) => {
    var socket = io.connect("http://localhost:5555/capture");

    socket.on('connect', function() {
      console.log('connected.');

      socket.emit('listen capture');
    });

    socket.on('receive capture', function(msg) {
      var blob = new Blob([msg.capture], {type: 'image/png'});

      capture = blob;
      title = msg.title;

      var url = URL.createObjectURL(blob);

      $('.capture-image').attr('src', url);

      var canvas = document.querySelector('.capture-image-modal .capture-image-canvas');
      var ctx = canvas.getContext('2d');
      var img = new Image();
      img.onload = function() {
        canvas.width = img.width;
        canvas.height = img.height;
        ctx.drawImage(img, 0, 0);

        $('.capture-image-modal').modal('show');
      }
      img.src = url;

      $('.capture-image').on('click', function(e) {
        var img = new Image();
        img.onload = function() {
          canvas.width = img.width;
          canvas.height = img.height;
          ctx.drawImage(img, 0, 0);

          if (startX !== undefined && startY !== undefined &&
              endX !== undefined && endY !== undefined) {
            ctx.strokeStyle = "#00ff00";
            ctx.lineWidth = 5;
            ctx.setLineDash([2, 3]);

            ctx.beginPath();

            ctx.moveTo(startX, startY);
            ctx.lineTo(endX, startY);

            ctx.moveTo(startX,endY);
            ctx.lineTo(endX,endY);

            ctx.moveTo(endX,startY);
            ctx.lineTo(endX,endY);

            ctx.moveTo(startX,startY);
            ctx.lineTo(startX,endY);

            ctx.stroke();
          }

          $('.capture-image-modal').modal('show');
        }
        img.src = url;
      });

      $('.capture-image-modal .modal-body').css('height', $(window).innerHeight() - 100);

      var isMouseDown = false;
      var startX, startY, endX, endY;
      $(canvas).on('mousedown', function(e) {
        isMouseDown = true;

        var rect = e.target.getBoundingClientRect();
        startX = e.clientX - rect.left;
        startY = e.clientY - rect.top;

        ctx.strokeStyle = "#00ff00";
        ctx.lineWidth = 5;
        ctx.setLineDash([2, 3]);
      }).on('mousemove', function(e) {
        if (!isMouseDown) {
          return;
        }

        var rect = e.target.getBoundingClientRect();
        endX = e.clientX - rect.left;
        endY = e.clientY - rect.top;

        ctx.drawImage(img, 0, 0);

        ctx.beginPath();

        ctx.moveTo(startX, startY);
        ctx.lineTo(endX, startY);

        ctx.moveTo(startX,endY);
        ctx.lineTo(endX,endY);

        ctx.moveTo(endX,startY);
        ctx.lineTo(endX,endY);

        ctx.moveTo(startX,startY);
        ctx.lineTo(startX,endY);

        ctx.stroke();
      }).on('mouseup', function(e) {
        isMouseDown = false;
      });

      $('.capture-image-modal .save-button').on('click', function() {
        rect = {
          "top": startY,
          "left": startX,
          "right": endX,
          "bottom": endY,
        };

        $('.capture-image-modal').modal('hide');
      });
    });

    location.href = 'openrpa:capture/' + $(e.currentTarget).attr('data-token');
  });

  $('#downloadButton').on('click', function() {
    var fd = new FormData();
    fd.append('capture', capture);
    fd.append('rect', JSON.stringify(rect));
    fd.append('title', title);

    var xhr = new XMLHttpRequest();
    xhr.onreadystatechange = function() {
      if (this.readyState === 4 && this.status === 200) {
        var z = new Blob([this.response], {type: 'application/zip'});

        location.href = URL.createObjectURL(z);
      }
    };
    xhr.open('POST', '/download');
    xhr.responseType = 'blob';
    xhr.send(fd);
  });
});
