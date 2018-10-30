import request from 'superagent';

export default {
  cmdReq: url=>{
      return request.get(`http://localhost:8001/${url}`).send().then(r=> {
          console.log('request returns');
          console.log(r);
      });
  }
};